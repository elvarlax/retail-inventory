using MediatR;

namespace RetailInventory.Application.Customers.Commands;

public record DeleteCustomerCommand(Guid Id) : IRequest;
