using AutoMapper;
using RetailInventory.Application.Common.Exceptions;
using RetailInventory.Application.Customers.Events;
using RetailInventory.Application.Interfaces;
using RetailInventory.Application.Outbox;
using RetailInventory.Domain;
using System.Text.Json;

namespace RetailInventory.Application.Customers.Commands;

public class CreateCustomerHandler
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IMapper _mapper;

    public CreateCustomerHandler(ICustomerRepository customerRepository, IOutboxRepository outboxRepository, IMapper mapper)
    {
        _customerRepository = customerRepository;
        _outboxRepository = outboxRepository;
        _mapper = mapper;
    }

    public async Task<Customer> Handle(CreateCustomerCommand command)
    {
        var existing = await _customerRepository.GetByEmailAsync(command.Email);
        if (existing != null)
            throw new ConflictException($"Email '{command.Email}' is already in use.");

        var customer = _mapper.Map<Customer>(command);
        customer.Id = Guid.NewGuid();

        await _customerRepository.AddAsync(customer);

        var occurredAt = DateTime.UtcNow;

        var @event = new CustomerCreatedV1
        {
            EventId = Guid.NewGuid(),
            OccurredAtUtc = occurredAt,
            CustomerId = customer.Id,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Email = customer.Email
        };

        await _outboxRepository.AddAsync(new OutboxEntry(
            Id: @event.EventId,
            Type: nameof(CustomerCreatedV1),
            Source: OutboxConstants.Source,
            Payload: JsonSerializer.Serialize(@event),
            OccurredAtUtc: occurredAt
        ));

        await _customerRepository.SaveChangesAsync();

        return customer;
    }
}
