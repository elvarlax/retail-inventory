using AutoMapper;
using RetailInventory.Api.DTOs;
using RetailInventory.Application.Authentication.Commands;

namespace RetailInventory.Api.Mappings;

public class ApiMappingProfile : Profile
{
    public ApiMappingProfile()
    {
        // API request DTOs → Application commands
        CreateMap<LoginRequestDto, LoginCommand>();
        CreateMap<RegisterRequestDto, RegisterCommand>();
    }
}
