using CabService.PassengerService.DTOs;

namespace CabService.PassengerService.Services;

public interface IPassengerService
{
    Task<PassengerDto?> GetPassengerByIdAsync(string id);
    Task<PassengerDto> CreatePassengerAsync(CreatePassengerDto createPassengerDto);
    Task<PassengerDto?> UpdatePassengerAsync(string id, UpdatePassengerDto updatePassengerDto);
    Task<bool> DeletePassengerAsync(string id);
    Task<PassengerProfileDto?> GetPassengerProfileAsync(string id);
    Task<bool> VerifyPassengerAsync(string id);
    Task<IEnumerable<PassengerDto>> SearchPassengersAsync(string? email, string? phoneNumber);
    Task<PassengerDto?> GetPassengerByEmailAsync(string email);
    Task<PassengerDto?> GetPassengerByPhoneAsync(string phoneNumber);
    Task UpdatePassengerRatingAsync(string passengerId, decimal newRating);
    Task IncrementTotalRidesAsync(string passengerId);
}
