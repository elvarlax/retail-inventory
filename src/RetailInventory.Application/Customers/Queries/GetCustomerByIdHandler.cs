using MediatR;
using RetailInventory.Application.Customers.DTOs;
using RetailInventory.Application.Interfaces;

namespace RetailInventory.Application.Customers.Queries;

public class GetCustomerByIdHandler : IRequestHandler<GetCustomerByIdQuery, CustomerDto?>
{
    private readonly ICustomerQueryRepository _queryRepository;

    public GetCustomerByIdHandler(ICustomerQueryRepository queryRepository)
    {
        _queryRepository = queryRepository;
    }

    public async Task<CustomerDto?> Handle(GetCustomerByIdQuery query, CancellationToken ct)
    {
        return await _queryRepository.GetByIdAsync(query.Id);
    }
}
