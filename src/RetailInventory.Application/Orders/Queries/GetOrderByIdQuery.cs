using MediatR;
using RetailInventory.Application.Orders.DTOs;

namespace RetailInventory.Application.Orders.Queries;

public record GetOrderByIdQuery(Guid Id) : IRequest<OrderDto>;
