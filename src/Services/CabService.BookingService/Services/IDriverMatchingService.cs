using CabService.BookingService.DTOs;
using CabService.Shared.Models;

namespace CabService.BookingService.Services;

public interface IDriverMatchingService
{
    Task<IEnumerable<NearbyDriverDto>> FindNearbyDriversAsync(double latitude, double longitude, VehicleType? vehicleType = null);
    Task<string?> FindBestDriverAsync(double latitude, double longitude, VehicleType vehicleType);
    Task<bool> AssignDriverToBookingAsync(string bookingId, string driverId);
    Task<double> CalculateDistanceAsync(double lat1, double lon1, double lat2, double lon2);
    Task<int> EstimateArrivalTimeAsync(double distanceKm);
}
