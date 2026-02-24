using AutoMapper;
using RetailInventory.Api.DTOs;
using RetailInventory.Api.Models;
using RetailInventory.Api.Repositories;

namespace RetailInventory.Api.Services;

public class CustomerService : ICustomerService
{
    private readonly IDummyJsonService _dummyService;
    private readonly ICustomerRepository _customerRepository;
    private readonly IMapper _mapper;

    public CustomerService(
        IDummyJsonService dummyService,
        ICustomerRepository customerRepository,
        IMapper mapper)
    {
        _dummyService = dummyService;
        _customerRepository = customerRepository;
        _mapper = mapper;
    }

    public async Task<int> ImportFromExternalAsync()
    {
        var users = await _dummyService.GetUsersAsync();
        var insertedCount = 0;

        foreach (var user in users)
        {
            var exists = await _customerRepository.ExistsByExternalIdAsync(user.Id);

            if (exists)
                continue;

            var customer = new Customer
            {
                Id = Guid.NewGuid(),
                ExternalId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email
            };

            await _customerRepository.AddAsync(customer);
            insertedCount++;
        }

        await _customerRepository.SaveChangesAsync();

        return insertedCount;
    }

    public async Task<List<CustomerDto>> GetAllAsync()
    {
        var customers = await _customerRepository.GetAllAsync();
        return _mapper.Map<List<CustomerDto>>(customers);
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