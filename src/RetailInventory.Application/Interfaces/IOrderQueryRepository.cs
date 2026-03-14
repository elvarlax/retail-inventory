using RetailInventory.Application.Orders.DTOs;
using RetailInventory.Domain;

namespace RetailInventory.Application.Interfaces;

public interface IOrderQueryRepository
{
    Task<int> CountAsync(OrderStatus? status, Guid? customerId, string? search = null);
    Task<OrderDto?> GetByIdAsync(Guid id);
    Task<List<OrderDto>> GetPagedAsync(int skip, int take, OrderStatus? status, string? sortBy, string? sortDirection, Guid? customerId, string? search = null);
    Task<OrderSummaryDto> GetSummaryAsync();
    Task<List<TopProductDto>> GetTopProductsAsync(int limit);
}
