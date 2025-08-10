using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Messaging.ServiceBus;
using CabService.Shared.Events;
using CabService.Shared.Infrastructure;
using CabService.Shared.Models;
using System.Text.Json;

namespace CabService.Functions.Functions;

public class DriverLocationFunction
{
    private readonly ILogger<DriverLocationFunction> _logger;
    private readonly ICosmosRepository<Driver> _driverRepository;
    private readonly IServiceBusPublisher _serviceBusPublisher;

    public DriverLocationFunction(
        ILogger<DriverLocationFunction> logger,
        ICosmosRepository<Driver> driverRepository,
        IServiceBusPublisher serviceBusPublisher)
    {
        _logger = logger;
        _driverRepository = driverRepository;
        _serviceBusPublisher = serviceBusPublisher;
    }

    [Function("ProcessDriverLocationUpdate")]
    public async Task ProcessDriverLocationUpdate(
        [ServiceBusTrigger("driver-events", "location-processor", Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message)
    {
        try
        {
            var messageBody = message.Body.ToString();
            var locationEvent = JsonSerializer.Deserialize<DriverLocationUpdatedEvent>(messageBody);

            if (locationEvent == null)
            {
                _logger.LogWarning("Failed to deserialize driver location event");
                return;
            }

            // Update driver location in database
            var driver = await _driverRepository.GetByIdAsync(locationEvent.DriverId, locationEvent.DriverId);
            if (driver != null)
            {
                driver.CurrentLocation = new Location
                {
                    Latitude = locationEvent.Latitude,
                    Longitude = locationEvent.Longitude,
                    Address = locationEvent.Address
                };
                driver.LastLocationUpdate = DateTime.UtcNow;
                driver.UpdatedAt = DateTime.UtcNow;

                await _driverRepository.UpdateAsync(driver);

                // Publish location update for nearby booking requests
                await _serviceBusPublisher.PublishAsync(locationEvent, "driver-location-updates");

                _logger.LogInformation("Updated location for driver {DriverId}", locationEvent.DriverId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing driver location update");
            throw;
        }
    }

    [Function("CleanupStaleDriverLocations")]
    public async Task CleanupStaleDriverLocations(
        [TimerTrigger("0 */5 * * * *")] TimerInfo timer) // Every 5 minutes
    {
        try
        {
            var staleThreshold = DateTime.UtcNow.AddMinutes(-10);
            var query = "SELECT * FROM c WHERE c.lastLocationUpdate < @threshold AND c.status = @status";
            var parameters = new { threshold = staleThreshold, status = "Online" };

            var staleDrivers = await _driverRepository.QueryAsync(query, parameters, "");

            foreach (var driver in staleDrivers)
            {
                driver.Status = DriverStatus.Offline;
                driver.UpdatedAt = DateTime.UtcNow;
                await _driverRepository.UpdateAsync(driver);

                // Publish driver went offline event
                var offlineEvent = new DriverWentOfflineEvent
                {
                    DriverId = driver.Id,
                    Timestamp = DateTime.UtcNow,
                    Reason = "Location timeout"
                };

                await _serviceBusPublisher.PublishAsync(offlineEvent, "driver-events");
            }

            _logger.LogInformation("Cleaned up {Count} stale driver locations", staleDrivers.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up stale driver locations");
        }
    }
}
