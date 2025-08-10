using AutoMapper;
using CabService.DriverService.DTOs;
using CabService.Shared.Models;

namespace CabService.DriverService.Mappings;

public class DriverMappingProfile : Profile
{
    public DriverMappingProfile()
    {
        CreateMap<Driver, DriverDto>();
        CreateMap<CreateDriverDto, Driver>();
        CreateMap<UpdateDriverDto, Driver>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        
        CreateMap<Driver, DriverProfileDto>()
            .ForMember(dest => dest.DriverSince, opt => opt.MapFrom(src => src.CreatedAt));
        
        CreateMap<Location, LocationDto>();
        CreateMap<LocationDto, Location>();
        
        CreateMap<Vehicle, VehicleDto>();
        CreateMap<VehicleDto, Vehicle>();
        
        CreateMap<BankAccount, BankAccountDto>();
        CreateMap<BankAccountDto, BankAccount>();
    }
}
