using Microsoft.EntityFrameworkCore.Storage;
using RetailInventory.Api.DTOs;
using RetailInventory.Api.Models;

public interface IOrderRepository
{
    Task AddAsync(Order order);
    Task<Order?> GetByIdAsync(Guid id);
    Task<int> CountAsync(OrderStatus? status);
    Task<List<Order>> GetPagedAsync(
        int skip,
        int take,
        OrderStatus? status,
        string? sortBy,
        string? sortDirection);
    Task SaveChangesAsync();
    Task<IDbContextTransaction> BeginTransactionAsync();
    Task<OrderSummaryDto> GetSummaryAsync();
}