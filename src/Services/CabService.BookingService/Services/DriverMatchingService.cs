using AutoMapper;
using CabService.BookingService.DTOs;
using CabService.Shared.Infrastructure;
using CabService.Shared.Models;

namespace CabService.BookingService.Services;

public class DriverMatchingService : IDriverMatchingService
{
    private readonly ICosmosRepository<Driver> _driverRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<DriverMatchingService> _logger;
    private readonly HttpClient _httpClient;

    public DriverMatchingService(
        ICosmosRepository<Driver> driverRepository,
        IMapper mapper,
        ILogger<DriverMatchingService> logger,
        HttpClient httpClient)
    {
        _driverRepository = driverRepository;
        _mapper = mapper;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<NearbyDriverDto>> FindNearbyDriversAsync(double latitude, double longitude, VehicleType? vehicleType = null)
    {
        try
        {
            // Get available drivers
            var query = "SELECT * FROM c WHERE c.status = @status AND c.isOnline = @isOnline AND c.isVerified = @isVerified AND c.isDeleted = false";
            object parameters = new { 
                status = DriverStatus.Available.ToString(), 
                isOnline = true, 
                isVerified = true 
            };

            if (vehicleType.HasValue)
            {
                query += " AND c.vehicle.type = @vehicleType";
                parameters = new { 
                    status = DriverStatus.Available.ToString(), 
                    isOnline = true, 
                    isVerified = true,
                    vehicleType = vehicleType.ToString()
                };
            }

            var drivers = await _driverRepository.QueryAsync(query, parameters, "");
            var nearbyDrivers = new List<NearbyDriverDto>();
            const double maxRadiusKm = 10.0; // Maximum search radius

            foreach (var driver in drivers)
            {
                if (driver.CurrentLocation != null)
                {
                    var distance = await CalculateDistanceAsync(
                        latitude, longitude,
                        driver.CurrentLocation.Latitude, driver.CurrentLocation.Longitude);

                    if (distance <= maxRadiusKm)
                    {
                        var estimatedArrival = await EstimateArrivalTimeAsync(distance);
                        
                        nearbyDrivers.Add(new NearbyDriverDto
                        {
                            Id = driver.Id,
                            FirstName = driver.FirstName,
                            LastName = driver.LastName,
                            CurrentLocation = _mapper.Map<LocationDto>(driver.CurrentLocation),
                            Vehicle = _mapper.Map<VehicleDto>(driver.Vehicle),
                            Rating = driver.Rating,
                            DistanceKm = Math.Round(distance, 2),
                            EstimatedArrivalMinutes = estimatedArrival
                        });
                    }
                }
            }

            // Sort by distance and rating
            return nearbyDrivers
                .OrderBy(d => d.DistanceKm)
                .ThenByDescending(d => d.Rating);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding nearby drivers for location ({Latitude}, {Longitude})", latitude, longitude);
            throw;
        }
    }

    public async Task<string?> FindBestDriverAsync(double latitude, double longitude, VehicleType vehicleType)
    {
        try
        {
            var nearbyDrivers = await FindNearbyDriversAsync(latitude, longitude, vehicleType);
            
            // Find the best driver based on distance, rating, and availability
            var bestDriver = nearbyDrivers
                .Where(d => d.DistanceKm <= 5.0) // Within 5km
                .OrderBy(d => d.DistanceKm * 0.7 + (5.0 - (double)d.Rating) * 0.3) // Weighted score
                .FirstOrDefault();

            return bestDriver?.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding best driver for location ({Latitude}, {Longitude})", latitude, longitude);
            throw;
        }
    }

    public async Task<bool> AssignDriverToBookingAsync(string bookingId, string driverId)
    {
        try
        {
            var driver = await _driverRepository.GetByIdAsync(driverId, driverId);
            if (driver == null || driver.Status != DriverStatus.Available)
            {
                return false;
            }

            // Update driver status to busy
            driver.Status = DriverStatus.Busy;
            driver.UpdatedAt = DateTime.UtcNow;
            await _driverRepository.UpdateAsync(driver);

            _logger.LogInformation("Assigned driver {DriverId} to booking {BookingId}", driverId, bookingId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning driver {DriverId} to booking {BookingId}", driverId, bookingId);
            throw;
        }
    }

    public async Task<double> CalculateDistanceAsync(double lat1, double lon1, double lat2, double lon2)
    {
        // Haversine formula to calculate distance between two points on Earth
        const double R = 6371; // Earth's radius in kilometers

        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        var distance = R * c;

        return await Task.FromResult(distance);
    }

    public async Task<int> EstimateArrivalTimeAsync(double distanceKm)
    {
        // Simple estimation: distance / speed * 60 (to get minutes)
        const double averageSpeedKmh = 30; // Average city driving speed
        var timeInHours = distanceKm / averageSpeedKmh;
        var timeInMinutes = (int)Math.Ceiling(timeInHours * 60);
        
        // Add some buffer time for traffic, stops, etc.
        var bufferMinutes = Math.Max(2, (int)(timeInMinutes * 0.2));
        
        return await Task.FromResult(timeInMinutes + bufferMinutes);
    }

    private static double ToRadians(double degrees)
    {
        return degrees * (Math.PI / 180);
    }
}
