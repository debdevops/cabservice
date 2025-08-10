using CabService.BookingService.DTOs;
using CabService.Shared.Models;

namespace CabService.BookingService.Services;

public class FareCalculationService : IFareCalculationService
{
    private readonly ILogger<FareCalculationService> _logger;
    private readonly IConfiguration _configuration;

    // Fare configuration - in production, these would come from configuration or database
    private readonly Dictionary<VehicleType, decimal> _baseFares = new()
    {
        { VehicleType.Hatchback, 2.50m },
        { VehicleType.Sedan, 3.00m },
        { VehicleType.SUV, 4.00m },
        { VehicleType.Luxury, 6.00m },
        { VehicleType.Electric, 3.50m },
        { VehicleType.Bike, 1.50m },
        { VehicleType.Auto, 2.00m }
    };

    private readonly Dictionary<VehicleType, decimal> _perKmRates = new()
    {
        { VehicleType.Hatchback, 1.20m },
        { VehicleType.Sedan, 1.50m },
        { VehicleType.SUV, 2.00m },
        { VehicleType.Luxury, 3.00m },
        { VehicleType.Electric, 1.30m },
        { VehicleType.Bike, 0.80m },
        { VehicleType.Auto, 1.00m }
    };

    private readonly Dictionary<VehicleType, decimal> _perMinuteRates = new()
    {
        { VehicleType.Hatchback, 0.25m },
        { VehicleType.Sedan, 0.30m },
        { VehicleType.SUV, 0.40m },
        { VehicleType.Luxury, 0.60m },
        { VehicleType.Electric, 0.28m },
        { VehicleType.Bike, 0.15m },
        { VehicleType.Auto, 0.20m }
    };

    public FareCalculationService(ILogger<FareCalculationService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<FareEstimateDto> CalculateFareAsync(LocationDto pickupLocation, LocationDto dropoffLocation, VehicleType vehicleType)
    {
        try
        {
            var distance = await CalculateDistanceAsync(pickupLocation, dropoffLocation);
            var duration = await EstimateDurationAsync(distance);
            var surgeMultiplier = await GetSurgePricingMultiplierAsync(pickupLocation, DateTime.UtcNow);

            var baseFare = _baseFares.GetValueOrDefault(vehicleType, 3.00m);
            var distanceFare = (decimal)distance * _perKmRates.GetValueOrDefault(vehicleType, 1.50m);
            var timeFare = (decimal)duration * _perMinuteRates.GetValueOrDefault(vehicleType, 0.30m);
            
            var subtotal = baseFare + distanceFare + timeFare;
            var surgePricing = subtotal * (surgeMultiplier - 1);
            var serviceFeePct = decimal.Parse(_configuration["Pricing:ServiceFeePercentage"] ?? "10");
            var taxPct = decimal.Parse(_configuration["Pricing:TaxPercentage"] ?? "8");
            
            var serviceFee = subtotal * (serviceFeePct / 100);
            var tax = (subtotal + serviceFee) * (taxPct / 100);
            
            var totalFare = subtotal + surgePricing + serviceFee + tax;

            var estimate = new FareEstimateDto
            {
                BaseFare = Math.Round(baseFare, 2),
                DistanceFare = Math.Round(distanceFare, 2),
                TimeFare = Math.Round(timeFare, 2),
                SurgePricing = Math.Round(surgePricing, 2),
                ServiceFee = Math.Round(serviceFee, 2),
                Tax = Math.Round(tax, 2),
                TotalEstimatedFare = Math.Round(totalFare, 2),
                EstimatedDistance = Math.Round(distance, 2),
                EstimatedDuration = duration,
                Currency = _configuration["Pricing:Currency"] ?? "USD"
            };

            _logger.LogInformation("Calculated fare estimate: {TotalFare} for distance {Distance}km, duration {Duration}min, vehicle {VehicleType}",
                estimate.TotalEstimatedFare, distance, duration, vehicleType);

            return estimate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating fare estimate for vehicle type {VehicleType}", vehicleType);
            throw;
        }
    }

    public async Task<decimal> CalculateActualFareAsync(double actualDistance, int actualDuration, VehicleType vehicleType)
    {
        try
        {
            var baseFare = _baseFares.GetValueOrDefault(vehicleType, 3.00m);
            var distanceFare = (decimal)actualDistance * _perKmRates.GetValueOrDefault(vehicleType, 1.50m);
            var timeFare = actualDuration * _perMinuteRates.GetValueOrDefault(vehicleType, 0.30m);
            
            var subtotal = baseFare + distanceFare + timeFare;
            var serviceFeePct = decimal.Parse(_configuration["Pricing:ServiceFeePercentage"] ?? "10");
            var taxPct = decimal.Parse(_configuration["Pricing:TaxPercentage"] ?? "8");
            
            var serviceFee = subtotal * (serviceFeePct / 100);
            var tax = (subtotal + serviceFee) * (taxPct / 100);
            
            var totalFare = subtotal + serviceFee + tax;

            _logger.LogInformation("Calculated actual fare: {TotalFare} for distance {Distance}km, duration {Duration}min, vehicle {VehicleType}",
                totalFare, actualDistance, actualDuration, vehicleType);

            return await Task.FromResult(Math.Round(totalFare, 2));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating actual fare for vehicle type {VehicleType}", vehicleType);
            throw;
        }
    }

    public async Task<double> CalculateDistanceAsync(LocationDto from, LocationDto to)
    {
        // Haversine formula to calculate distance between two points on Earth
        const double R = 6371; // Earth's radius in kilometers

        var dLat = ToRadians(to.Latitude - from.Latitude);
        var dLon = ToRadians(to.Longitude - from.Longitude);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(from.Latitude)) * Math.Cos(ToRadians(to.Latitude)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        var distance = R * c;

        // Add some buffer for actual road distance (typically 20-30% more than straight line)
        var roadDistance = distance * 1.25;

        return await Task.FromResult(roadDistance);
    }

    public async Task<int> EstimateDurationAsync(double distanceKm)
    {
        // Estimate duration based on distance and average city speed
        const double averageSpeedKmh = 25; // Average city driving speed including traffic
        var timeInHours = distanceKm / averageSpeedKmh;
        var timeInMinutes = (int)Math.Ceiling(timeInHours * 60);
        
        // Minimum trip time
        var minimumMinutes = 5;
        
        return await Task.FromResult(Math.Max(timeInMinutes, minimumMinutes));
    }

    public async Task<decimal> GetSurgePricingMultiplierAsync(LocationDto location, DateTime requestTime)
    {
        try
        {
            // Simple surge pricing logic based on time of day
            // In production, this would be more sophisticated based on demand/supply analysis
            
            var hour = requestTime.Hour;
            decimal surgeMultiplier = 1.0m;

            // Peak hours surge pricing
            if ((hour >= 7 && hour <= 9) || (hour >= 17 && hour <= 19))
            {
                surgeMultiplier = 1.5m; // 50% surge during peak hours
            }
            else if ((hour >= 22 && hour <= 23) || (hour >= 0 && hour <= 2))
            {
                surgeMultiplier = 1.3m; // 30% surge during late night
            }
            else if (hour >= 3 && hour <= 5)
            {
                surgeMultiplier = 2.0m; // 100% surge during very late night/early morning
            }

            // Weekend surge (simplified - would need more complex date logic in production)
            if (requestTime.DayOfWeek == DayOfWeek.Friday || requestTime.DayOfWeek == DayOfWeek.Saturday)
            {
                if (hour >= 20 || hour <= 2)
                {
                    surgeMultiplier = Math.Max(surgeMultiplier, 1.8m); // Weekend night surge
                }
            }

            _logger.LogInformation("Applied surge multiplier {SurgeMultiplier} for time {RequestTime}", 
                surgeMultiplier, requestTime);

            return await Task.FromResult(surgeMultiplier);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating surge pricing multiplier");
            return await Task.FromResult(1.0m); // Default to no surge on error
        }
    }

    private static double ToRadians(double degrees)
    {
        return degrees * (Math.PI / 180);
    }
}
