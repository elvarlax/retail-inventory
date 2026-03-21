using MediatR;
using RetailInventory.Domain;

namespace RetailInventory.Application.Customers.Commands;

public record CreateCustomerCommand(
    string FirstName,
    string LastName,
    string Email
) : IRequest<Customer>;
