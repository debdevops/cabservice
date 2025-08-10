using CabService.BookingService.DTOs;
using CabService.Shared.Models;

namespace CabService.BookingService.Services;

public interface IBookingService
{
    Task<BookingDto> CreateBookingAsync(CreateBookingDto createBookingDto);
    Task<BookingDto?> GetBookingByIdAsync(string id);
    Task<bool> CancelBookingAsync(string id, string reason, string cancelledBy);
    Task<bool> AcceptBookingAsync(string id, string driverId);
    Task<bool> StartTripAsync(string id, string driverId, LocationDto startLocation);
    Task<bool> CompleteTripAsync(string id, string driverId, CompleteTripDto completeDto);
    Task<IEnumerable<BookingDto>> GetPassengerBookingsAsync(string passengerId);
    Task<IEnumerable<BookingDto>> GetDriverBookingsAsync(string driverId);
    Task<bool> RateTripAsync(string id, RateTripDto rateDto);
    Task<bool> UpdateBookingStatusAsync(string id, BookingStatus status);
    Task<BookingDto?> GetActiveBookingForPassengerAsync(string passengerId);
    Task<BookingDto?> GetActiveBookingForDriverAsync(string driverId);
}
