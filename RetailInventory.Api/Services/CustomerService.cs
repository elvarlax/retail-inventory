using AutoMapper;
using RetailInventory.Api.DTOs;
using RetailInventory.Api.Repositories;

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
