using CabService.DriverService.DTOs;
using CabService.Shared.Models;

namespace CabService.DriverService.Services;

public interface ILocationService
{
    Task UpdateDriverLocationAsync(string driverId, LocationUpdateDto locationDto);
    Task<IEnumerable<NearbyDriverDto>> GetNearbyDriversAsync(double latitude, double longitude, double radiusKm, VehicleType? vehicleType = null);
    Task<LocationDto?> GetDriverLocationAsync(string driverId);
    Task<double> CalculateDistanceAsync(double lat1, double lon1, double lat2, double lon2);
    Task<int> EstimateArrivalTimeAsync(double distanceKm, double averageSpeedKmh = 30);
}
