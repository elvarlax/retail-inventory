using MediatR;
using RetailInventory.Application.Orders.DTOs;

namespace RetailInventory.Application.Orders.Queries;

public record GetTopProductsQuery(int Limit = 5) : IRequest<List<TopProductDto>>;
