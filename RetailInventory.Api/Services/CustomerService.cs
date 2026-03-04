using AutoMapper;
using RetailInventory.Api.DTOs;
using RetailInventory.Api.Events;
using RetailInventory.Api.Models;
using RetailInventory.Api.Repositories;
using System.Text.Json;

namespace RetailInventory.Api.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IMapper _mapper;

    public CustomerService(ICustomerRepository customerRepository, IMapper mapper)
    {
        _customerRepository = customerRepository;
        _mapper = mapper;
    }

    public async Task<Customer> CreateAsync(RegisterRequestDto request)
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email
        };

        await _customerRepository.AddAsync(customer);

        // Create event
        var occurredAt = DateTime.UtcNow;

        var customerCreatedEvent = new CustomerCreatedV1
        {
            EventId = Guid.NewGuid(),
            OccurredAtUtc = occurredAt,
            CustomerId = customer.Id,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Email = customer.Email
        };

        await _customerRepository.AddOutboxMessageAsync(new OutboxMessage
        {
            Id = customerCreatedEvent.EventId,
            Type = nameof(CustomerCreatedV1),
            Source = OutboxConstants.Source,
            Payload = JsonSerializer.Serialize(customerCreatedEvent),
            OccurredAtUtc = occurredAt
        });

        await _customerRepository.SaveChangesAsync();

        return customer;
    }

    public async Task<CustomerDto?> GetByIdAsync(Guid id)
    {
        var customer = await _customerRepository.GetByIdAsync(id);

        if (customer == null)
            return null;

        return _mapper.Map<CustomerDto>(customer);
    }

    public async Task<PagedResultDto<CustomerDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? sortBy,
        string? sortDirection)
    {
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0 || pageSize > 50) pageSize = 10;

        var skip = (pageNumber - 1) * pageSize;

        var totalCount = await _customerRepository.CountAsync();
        var customers = await _customerRepository.GetPagedAsync(skip, pageSize, sortBy, sortDirection);

        return new PagedResultDto<CustomerDto>
        {
            Items = _mapper.Map<List<CustomerDto>>(customers),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}
