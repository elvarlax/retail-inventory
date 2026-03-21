using MediatR;
using RetailInventory.Application.Products.DTOs;

namespace RetailInventory.Application.Products.Queries;

public record GetProductByIdQuery(Guid Id) : IRequest<ProductDto?>;
