using AutoMapper;
using CabService.BookingService.DTOs;
using CabService.Shared.Models;

namespace CabService.BookingService.Mappings;

public class BookingMappingProfile : Profile
{
    public BookingMappingProfile()
    {
        CreateMap<Booking, BookingDto>();
        CreateMap<CreateBookingDto, Booking>();
        
        CreateMap<Location, LocationDto>();
        CreateMap<LocationDto, Location>();
        
        CreateMap<Rating, RatingDto>();
        CreateMap<RatingDto, Rating>();
        
        CreateMap<Vehicle, VehicleDto>();
        CreateMap<VehicleDto, Vehicle>();
        
        CreateMap<Driver, NearbyDriverDto>()
            .ForMember(dest => dest.CurrentLocation, opt => opt.MapFrom(src => src.CurrentLocation))
            .ForMember(dest => dest.Vehicle, opt => opt.MapFrom(src => src.Vehicle))
            .ForMember(dest => dest.DistanceKm, opt => opt.Ignore())
            .ForMember(dest => dest.EstimatedArrivalMinutes, opt => opt.Ignore());
    }
}
