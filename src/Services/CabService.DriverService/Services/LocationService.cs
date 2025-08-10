using AutoMapper;
using CabService.DriverService.DTOs;
using CabService.Shared.Infrastructure;
using CabService.Shared.Models;
using CabService.Shared.Events;

namespace CabService.DriverService.Services;

public class LocationService : ILocationService
{
    private readonly ICosmosRepository<Driver> _driverRepository;
    private readonly IServiceBusPublisher _serviceBusPublisher;
    private readonly IMapper _mapper;
    private readonly ILogger<LocationService> _logger;

    public LocationService(
        ICosmosRepository<Driver> driverRepository,
        IServiceBusPublisher serviceBusPublisher,
        IMapper mapper,
        ILogger<LocationService> logger)
    {
        _driverRepository = driverRepository;
        _serviceBusPublisher = serviceBusPublisher;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task UpdateDriverLocationAsync(string driverId, LocationUpdateDto locationDto)
    {
        try
        {
            var driver = await _driverRepository.GetByIdAsync(driverId, driverId);
            if (driver == null)
            {
                throw new ArgumentException($"Driver with ID {driverId} not found");
            }

            var location = new Location
            {
                Latitude = locationDto.Latitude,
                Longitude = locationDto.Longitude,
                Address = locationDto.Address,
                Timestamp = DateTime.UtcNow
            };

            driver.CurrentLocation = location;
            driver.LastLocationUpdate = DateTime.UtcNow;
            driver.UpdatedAt = DateTime.UtcNow;

            await _driverRepository.UpdateAsync(driver);

            // Publish driver location updated event
            var locationUpdatedEvent = new DriverLocationUpdatedEvent
            {
                DriverId = driver.Id,
                CurrentLocation = location,
                Status = driver.Status,
                IsOnline = driver.IsOnline,
                Source = "DriverService",
                UserId = driver.Id,
                CorrelationId = Guid.NewGuid().ToString()
            };

            await _serviceBusPublisher.PublishAsync(locationUpdatedEvent, "driver-events");

            _logger.LogInformation("Updated location for driver {DriverId} to ({Latitude}, {Longitude})", 
                driverId, locationDto.Latitude, locationDto.Longitude);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating location for driver {DriverId}", driverId);
            throw;
        }
    }

    public async Task<IEnumerable<NearbyDriverDto>> GetNearbyDriversAsync(double latitude, double longitude, double radiusKm, VehicleType? vehicleType = null)
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

            foreach (var driver in drivers)
            {
                if (driver.CurrentLocation != null)
                {
                    var distance = await CalculateDistanceAsync(
                        latitude, longitude,
                        driver.CurrentLocation.Latitude, driver.CurrentLocation.Longitude);

                    if (distance <= radiusKm)
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

            // Sort by distance
            return nearbyDrivers.OrderBy(d => d.DistanceKm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting nearby drivers for location ({Latitude}, {Longitude})", latitude, longitude);
            throw;
        }
    }

    public async Task<LocationDto?> GetDriverLocationAsync(string driverId)
    {
        try
        {
            var driver = await _driverRepository.GetByIdAsync(driverId, driverId);
            if (driver?.CurrentLocation == null)
            {
                return null;
            }

            return _mapper.Map<LocationDto>(driver.CurrentLocation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting location for driver {DriverId}", driverId);
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

    public async Task<int> EstimateArrivalTimeAsync(double distanceKm, double averageSpeedKmh = 30)
    {
        // Simple estimation: distance / speed * 60 (to get minutes)
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
