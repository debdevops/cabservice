using AutoMapper;
using CabService.PassengerService.DTOs;
using CabService.Shared.Models;

namespace CabService.PassengerService.Mappings;

public class PassengerMappingProfile : Profile
{
    public PassengerMappingProfile()
    {
        CreateMap<Passenger, PassengerDto>();
        CreateMap<CreatePassengerDto, Passenger>();
        CreateMap<UpdatePassengerDto, Passenger>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        
        CreateMap<Passenger, PassengerProfileDto>()
            .ForMember(dest => dest.MemberSince, opt => opt.MapFrom(src => src.CreatedAt));
        
        CreateMap<Address, AddressDto>();
        CreateMap<AddressDto, Address>();
    }
}
