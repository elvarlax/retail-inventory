using AutoMapper;
using RetailInventory.Api.DTOs;
using RetailInventory.Api.Models;

namespace RetailInventory.Api.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Product
        CreateMap<Product, ProductDto>();

        // Customer
        CreateMap<Customer, CustomerDto>();

        // OrderItems
        CreateMap<OrderItem, OrderItemDto>();

        // Orders
        CreateMap<Order, OrderDto>()
            .ForMember(dest => dest.Status,
                opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.Items,
                opt => opt.MapFrom(src => src.OrderItems));
    }
}