using RetailInventory.Api.DTOs;

namespace RetailInventory.Api.Services;

public interface IOrderService
{
    Task<Guid> CreateAsync(CreateOrderRequest request);
    Task<OrderDto> GetByIdAsync(Guid id);
    Task CompleteAsync(Guid id);
    Task CancelAsync(Guid id);
    Task<OrderSummaryDto> GetSummaryAsync();
    Task<PagedResultDto<OrderDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? status,
        string? sortBy,
        string? sortDirection);
}