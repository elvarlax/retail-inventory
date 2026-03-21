using MediatR;
using RetailInventory.Application.Orders.DTOs;

namespace RetailInventory.Application.Orders.Queries;

public record GetOrderSummaryQuery : IRequest<OrderSummaryDto>;
