using CabService.DriverService.DTOs;
using CabService.Shared.Models;

namespace CabService.DriverService.Services;

public interface IDriverService
{
    Task<DriverDto?> GetDriverByIdAsync(string id);
    Task<DriverDto> CreateDriverAsync(CreateDriverDto createDriverDto);
    Task<DriverDto?> UpdateDriverAsync(string id, UpdateDriverDto updateDriverDto);
    Task<bool> DeleteDriverAsync(string id);
    Task<DriverProfileDto?> GetDriverProfileAsync(string id);
    Task<bool> VerifyDriverAsync(string id);
    Task<bool> UpdateDriverStatusAsync(string id, DriverStatus status);
    Task<IEnumerable<DriverDto>> GetAvailableDriversAsync();
    Task<DriverDto?> GetDriverByEmailAsync(string email);
    Task<DriverDto?> GetDriverByPhoneAsync(string phoneNumber);
    Task UpdateDriverRatingAsync(string driverId, decimal newRating);
    Task IncrementTotalRidesAsync(string driverId);
    Task SetDriverOnlineAsync(string driverId, bool isOnline);
}
