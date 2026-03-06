using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using RetailInventory.Api.Data;

namespace RetailInventory.Api.Services;

public class OutboxPublisher : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;
    private readonly ILogger<OutboxPublisher> _logger;


    public OutboxPublisher(
        IServiceProvider serviceProvider,
        IConfiguration config,
        ILogger<OutboxPublisher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        var connectionString = config["ServiceBus:ConnectionString"]
            ?? throw new InvalidOperationException("ServiceBus:ConnectionString is missing from configuration.");
        var topic = config["ServiceBus:TopicName"]
            ?? throw new InvalidOperationException("ServiceBus:TopicName is missing from configuration.");

        _client = new ServiceBusClient(connectionString);
        _sender = _client.CreateSender(topic);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxPublisher started");

        var consecutiveFailures = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<RetailDbContext>();

                var messages = await dbContext.OutboxMessages
                    .Where(x => x.PublishedAtUtc == null)
                    .OrderBy(x => x.OccurredAtUtc)
                    .Take(20)
                    .ToListAsync(stoppingToken);

                if (messages.Count == 0)
                {
                    consecutiveFailures = 0;
                    await Task.Delay(3000, stoppingToken);
                    continue;
                }

                var sbMessages = new List<ServiceBusMessage>();

                foreach (var message in messages)
                {
                    var sbMessage = new ServiceBusMessage(message.Payload)
                    {
                        MessageId = message.Id.ToString(),
                        Subject = message.Type
                    };

                    sbMessage.ApplicationProperties["source"] = message.Source;
                    sbMessages.Add(sbMessage);
                }

                await _sender.SendMessagesAsync(sbMessages, stoppingToken);

                var now = DateTime.UtcNow;
                foreach (var message in messages)
                    message.PublishedAtUtc = now;

                await dbContext.SaveChangesAsync(stoppingToken);

                _logger.LogInformation("Published {Count} outbox message(s)", messages.Count);

                consecutiveFailures = 0;
            }
            catch (Exception ex)
            {
                consecutiveFailures++;
                var delaySecs = Math.Min(5 * consecutiveFailures, 60);

                _logger.LogError(ex,
                    "Outbox publishing failed (attempt {Attempt}), retrying in {Delay}s",
                    consecutiveFailures,
                    delaySecs);

                await Task.Delay(TimeSpan.FromSeconds(delaySecs), stoppingToken);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _sender.DisposeAsync();
        await _client.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }
}
