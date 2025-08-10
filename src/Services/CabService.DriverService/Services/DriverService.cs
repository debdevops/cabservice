using AutoMapper;
using CabService.DriverService.DTOs;
using CabService.Shared.Infrastructure;
using CabService.Shared.Models;
using CabService.Shared.Events;

namespace CabService.DriverService.Services;

public class DriverService : IDriverService
{
    private readonly ICosmosRepository<Driver> _driverRepository;
    private readonly IServiceBusPublisher _serviceBusPublisher;
    private readonly IMapper _mapper;
    private readonly ILogger<DriverService> _logger;

    public DriverService(
        ICosmosRepository<Driver> driverRepository,
        IServiceBusPublisher serviceBusPublisher,
        IMapper mapper,
        ILogger<DriverService> logger)
    {
        _driverRepository = driverRepository;
        _serviceBusPublisher = serviceBusPublisher;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<DriverDto?> GetDriverByIdAsync(string id)
    {
        try
        {
            var driver = await _driverRepository.GetByIdAsync(id, id);
            return driver != null ? _mapper.Map<DriverDto>(driver) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting driver with ID {DriverId}", id);
            throw;
        }
    }

    public async Task<DriverDto> CreateDriverAsync(CreateDriverDto createDriverDto)
    {
        try
        {
            var driver = _mapper.Map<Driver>(createDriverDto);
            driver.Id = Guid.NewGuid().ToString();
            driver.CreatedAt = DateTime.UtcNow;
            driver.UpdatedAt = DateTime.UtcNow;
            driver.Status = DriverStatus.Offline;

            var createdDriver = await _driverRepository.CreateAsync(driver);

            // Publish driver registered event
            var driverRegisteredEvent = new DriverRegisteredEvent
            {
                DriverId = createdDriver.Id,
                FirstName = createdDriver.FirstName,
                LastName = createdDriver.LastName,
                Email = createdDriver.Email,
                PhoneNumber = createdDriver.PhoneNumber,
                LicenseNumber = createdDriver.LicenseNumber,
                Vehicle = createdDriver.Vehicle ?? new Vehicle(),
                Source = "DriverService",
                UserId = createdDriver.Id,
                CorrelationId = Guid.NewGuid().ToString()
            };

            await _serviceBusPublisher.PublishAsync(driverRegisteredEvent, "driver-events");

            _logger.LogInformation("Created driver with ID {DriverId}", createdDriver.Id);
            return _mapper.Map<DriverDto>(createdDriver);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating driver");
            throw;
        }
    }

    public async Task<DriverDto?> UpdateDriverAsync(string id, UpdateDriverDto updateDriverDto)
    {
        try
        {
            var existingDriver = await _driverRepository.GetByIdAsync(id, id);
            if (existingDriver == null)
            {
                return null;
            }

            // Update only provided fields
            if (!string.IsNullOrEmpty(updateDriverDto.FirstName))
                existingDriver.FirstName = updateDriverDto.FirstName;
            
            if (!string.IsNullOrEmpty(updateDriverDto.LastName))
                existingDriver.LastName = updateDriverDto.LastName;
            
            if (!string.IsNullOrEmpty(updateDriverDto.Email))
                existingDriver.Email = updateDriverDto.Email;
            
            if (!string.IsNullOrEmpty(updateDriverDto.PhoneNumber))
                existingDriver.PhoneNumber = updateDriverDto.PhoneNumber;
            
            if (updateDriverDto.DateOfBirth.HasValue)
                existingDriver.DateOfBirth = updateDriverDto.DateOfBirth;
            
            if (!string.IsNullOrEmpty(updateDriverDto.ProfilePictureUrl))
                existingDriver.ProfilePictureUrl = updateDriverDto.ProfilePictureUrl;
            
            if (!string.IsNullOrEmpty(updateDriverDto.LicenseNumber))
                existingDriver.LicenseNumber = updateDriverDto.LicenseNumber;
            
            if (updateDriverDto.LicenseExpiryDate.HasValue)
                existingDriver.LicenseExpiryDate = updateDriverDto.LicenseExpiryDate.Value;
            
            if (updateDriverDto.Vehicle != null)
                existingDriver.Vehicle = _mapper.Map<Vehicle>(updateDriverDto.Vehicle);
            
            if (updateDriverDto.BankAccount != null)
                existingDriver.BankAccount = _mapper.Map<BankAccount>(updateDriverDto.BankAccount);

            existingDriver.UpdatedAt = DateTime.UtcNow;

            var updatedDriver = await _driverRepository.UpdateAsync(existingDriver);

            _logger.LogInformation("Updated driver with ID {DriverId}", id);
            return _mapper.Map<DriverDto>(updatedDriver);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating driver with ID {DriverId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteDriverAsync(string id)
    {
        try
        {
            var driver = await _driverRepository.GetByIdAsync(id, id);
            if (driver == null)
            {
                return false;
            }

            await _driverRepository.DeleteAsync(id, id);

            _logger.LogInformation("Deleted driver with ID {DriverId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting driver with ID {DriverId}", id);
            throw;
        }
    }

    public async Task<DriverProfileDto?> GetDriverProfileAsync(string id)
    {
        try
        {
            var driver = await _driverRepository.GetByIdAsync(id, id);
            if (driver == null)
            {
                return null;
            }

            var profile = _mapper.Map<DriverProfileDto>(driver);
            profile.DriverSince = driver.CreatedAt;

            return profile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting driver profile with ID {DriverId}", id);
            throw;
        }
    }

    public async Task<bool> VerifyDriverAsync(string id)
    {
        try
        {
            var driver = await _driverRepository.GetByIdAsync(id, id);
            if (driver == null)
            {
                return false;
            }

            driver.IsVerified = true;
            driver.VerifiedAt = DateTime.UtcNow;
            driver.UpdatedAt = DateTime.UtcNow;

            await _driverRepository.UpdateAsync(driver);

            // Publish driver verified event
            var driverVerifiedEvent = new DriverVerifiedEvent
            {
                DriverId = driver.Id,
                VerifiedAt = driver.VerifiedAt.Value,
                VerifiedBy = "System", // This could be the admin user ID
                Source = "DriverService",
                UserId = driver.Id,
                CorrelationId = Guid.NewGuid().ToString()
            };

            await _serviceBusPublisher.PublishAsync(driverVerifiedEvent, "driver-events");

            _logger.LogInformation("Verified driver with ID {DriverId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying driver with ID {DriverId}", id);
            throw;
        }
    }

    public async Task<bool> UpdateDriverStatusAsync(string id, DriverStatus status)
    {
        try
        {
            var driver = await _driverRepository.GetByIdAsync(id, id);
            if (driver == null)
            {
                return false;
            }

            var previousStatus = driver.Status;
            driver.Status = status;
            driver.UpdatedAt = DateTime.UtcNow;

            if (status == DriverStatus.Available)
            {
                driver.IsOnline = true;
                driver.LastOnlineAt = DateTime.UtcNow;
            }
            else if (status == DriverStatus.Offline)
            {
                driver.IsOnline = false;
            }

            await _driverRepository.UpdateAsync(driver);

            // Publish driver status changed event
            var statusChangedEvent = new DriverStatusChangedEvent
            {
                DriverId = driver.Id,
                PreviousStatus = previousStatus,
                NewStatus = status,
                CurrentLocation = driver.CurrentLocation,
                Source = "DriverService",
                UserId = driver.Id,
                CorrelationId = Guid.NewGuid().ToString()
            };

            await _serviceBusPublisher.PublishAsync(statusChangedEvent, "driver-events");

            _logger.LogInformation("Updated driver status for {DriverId} from {PreviousStatus} to {NewStatus}", 
                id, previousStatus, status);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating driver status with ID {DriverId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<DriverDto>> GetAvailableDriversAsync()
    {
        try
        {
            var query = "SELECT * FROM c WHERE c.status = @status AND c.isOnline = @isOnline AND c.isVerified = @isVerified AND c.isDeleted = false";
            var parameters = new { 
                status = DriverStatus.Available.ToString(), 
                isOnline = true, 
                isVerified = true 
            };
            
            var drivers = await _driverRepository.QueryAsync(query, parameters, "");
            return _mapper.Map<IEnumerable<DriverDto>>(drivers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available drivers");
            throw;
        }
    }

    public async Task<DriverDto?> GetDriverByEmailAsync(string email)
    {
        try
        {
            var query = "SELECT * FROM c WHERE c.email = @email AND c.isDeleted = false";
            var parameters = new { email };
            
            var drivers = await _driverRepository.QueryAsync(query, parameters, "");
            var driver = drivers.FirstOrDefault();
            
            return driver != null ? _mapper.Map<DriverDto>(driver) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting driver by email {Email}", email);
            throw;
        }
    }

    public async Task<DriverDto?> GetDriverByPhoneAsync(string phoneNumber)
    {
        try
        {
            var query = "SELECT * FROM c WHERE c.phoneNumber = @phoneNumber AND c.isDeleted = false";
            var parameters = new { phoneNumber };
            
            var drivers = await _driverRepository.QueryAsync(query, parameters, "");
            var driver = drivers.FirstOrDefault();
            
            return driver != null ? _mapper.Map<DriverDto>(driver) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting driver by phone {PhoneNumber}", phoneNumber);
            throw;
        }
    }

    public async Task UpdateDriverRatingAsync(string driverId, decimal newRating)
    {
        try
        {
            var driver = await _driverRepository.GetByIdAsync(driverId, driverId);
            if (driver != null)
            {
                driver.Rating = newRating;
                driver.UpdatedAt = DateTime.UtcNow;
                await _driverRepository.UpdateAsync(driver);
                
                _logger.LogInformation("Updated rating for driver {DriverId} to {Rating}", driverId, newRating);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating rating for driver {DriverId}", driverId);
            throw;
        }
    }

    public async Task IncrementTotalRidesAsync(string driverId)
    {
        try
        {
            var driver = await _driverRepository.GetByIdAsync(driverId, driverId);
            if (driver != null)
            {
                driver.TotalRides++;
                driver.UpdatedAt = DateTime.UtcNow;
                await _driverRepository.UpdateAsync(driver);
                
                _logger.LogInformation("Incremented total rides for driver {DriverId} to {TotalRides}", driverId, driver.TotalRides);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing total rides for driver {DriverId}", driverId);
            throw;
        }
    }

    public async Task SetDriverOnlineAsync(string driverId, bool isOnline)
    {
        try
        {
            var driver = await _driverRepository.GetByIdAsync(driverId, driverId);
            if (driver != null)
            {
                driver.IsOnline = isOnline;
                driver.UpdatedAt = DateTime.UtcNow;
                
                if (isOnline)
                {
                    driver.LastOnlineAt = DateTime.UtcNow;
                    driver.Status = DriverStatus.Available;
                    
                    // Publish driver online event
                    var onlineEvent = new DriverOnlineEvent
                    {
                        DriverId = driver.Id,
                        CurrentLocation = driver.CurrentLocation ?? new Location(),
                        OnlineAt = DateTime.UtcNow,
                        Source = "DriverService",
                        UserId = driver.Id,
                        CorrelationId = Guid.NewGuid().ToString()
                    };
                    
                    await _serviceBusPublisher.PublishAsync(onlineEvent, "driver-events");
                }
                else
                {
                    driver.Status = DriverStatus.Offline;
                    
                    // Publish driver offline event
                    var offlineEvent = new DriverOfflineEvent
                    {
                        DriverId = driver.Id,
                        LastKnownLocation = driver.CurrentLocation,
                        OfflineAt = DateTime.UtcNow,
                        Source = "DriverService",
                        UserId = driver.Id,
                        CorrelationId = Guid.NewGuid().ToString()
                    };
                    
                    await _serviceBusPublisher.PublishAsync(offlineEvent, "driver-events");
                }
                
                await _driverRepository.UpdateAsync(driver);
                
                _logger.LogInformation("Set driver {DriverId} online status to {IsOnline}", driverId, isOnline);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting online status for driver {DriverId}", driverId);
            throw;
        }
    }
}
