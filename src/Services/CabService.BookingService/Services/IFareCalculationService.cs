using CabService.BookingService.DTOs;
using CabService.Shared.Models;

namespace CabService.BookingService.Services;

public interface IFareCalculationService
{
    Task<FareEstimateDto> CalculateFareAsync(LocationDto pickupLocation, LocationDto dropoffLocation, VehicleType vehicleType);
    Task<decimal> CalculateActualFareAsync(double actualDistance, int actualDuration, VehicleType vehicleType);
    Task<double> CalculateDistanceAsync(LocationDto from, LocationDto to);
    Task<int> EstimateDurationAsync(double distanceKm);
    Task<decimal> GetSurgePricingMultiplierAsync(LocationDto location, DateTime requestTime);
}
