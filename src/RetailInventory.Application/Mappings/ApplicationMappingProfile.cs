using AutoMapper;
using RetailInventory.Application.Customers.Commands;
using RetailInventory.Application.Products.Commands;
using RetailInventory.Domain;

namespace RetailInventory.Application.Mappings;

public class ApplicationMappingProfile : Profile
{
    public ApplicationMappingProfile()
    {
        // Command → Domain entity (Id is assigned by the handler, not AutoMapper)
        CreateMap<CreateProductCommand, Product>()
            .ForMember(dest => dest.Id, opt => opt.Ignore());

        CreateMap<CreateCustomerCommand, Customer>()
            .ForMember(dest => dest.Id, opt => opt.Ignore());
    }
}
