using AutoMapper;
using CabService.PassengerService.DTOs;
using CabService.Shared.Infrastructure;
using CabService.Shared.Models;
using CabService.Shared.Events;

namespace CabService.PassengerService.Services;

public class PassengerService : IPassengerService
{
    private readonly ICosmosRepository<Passenger> _passengerRepository;
    private readonly IServiceBusPublisher _serviceBusPublisher;
    private readonly IMapper _mapper;
    private readonly ILogger<PassengerService> _logger;

    public PassengerService(
        ICosmosRepository<Passenger> passengerRepository,
        IServiceBusPublisher serviceBusPublisher,
        IMapper mapper,
        ILogger<PassengerService> logger)
    {
        _passengerRepository = passengerRepository;
        _serviceBusPublisher = serviceBusPublisher;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PassengerDto?> GetPassengerByIdAsync(string id)
    {
        try
        {
            var passenger = await _passengerRepository.GetByIdAsync(id, id);
            return passenger != null ? _mapper.Map<PassengerDto>(passenger) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting passenger with ID {PassengerId}", id);
            throw;
        }
    }

    public async Task<PassengerDto> CreatePassengerAsync(CreatePassengerDto createPassengerDto)
    {
        try
        {
            var passenger = _mapper.Map<Passenger>(createPassengerDto);
            passenger.Id = Guid.NewGuid().ToString();
            passenger.CreatedAt = DateTime.UtcNow;
            passenger.UpdatedAt = DateTime.UtcNow;

            var createdPassenger = await _passengerRepository.CreateAsync(passenger);

            // Publish passenger registered event
            var passengerRegisteredEvent = new PassengerRegisteredEvent
            {
                PassengerId = createdPassenger.Id,
                FirstName = createdPassenger.FirstName,
                LastName = createdPassenger.LastName,
                Email = createdPassenger.Email,
                PhoneNumber = createdPassenger.PhoneNumber,
                Source = "PassengerService",
                UserId = createdPassenger.Id,
                CorrelationId = Guid.NewGuid().ToString()
            };

            await _serviceBusPublisher.PublishAsync(passengerRegisteredEvent, "passenger-events");

            _logger.LogInformation("Created passenger with ID {PassengerId}", createdPassenger.Id);
            return _mapper.Map<PassengerDto>(createdPassenger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating passenger");
            throw;
        }
    }

    public async Task<PassengerDto?> UpdatePassengerAsync(string id, UpdatePassengerDto updatePassengerDto)
    {
        try
        {
            var existingPassenger = await _passengerRepository.GetByIdAsync(id, id);
            if (existingPassenger == null)
            {
                return null;
            }

            // Update only provided fields
            if (!string.IsNullOrEmpty(updatePassengerDto.FirstName))
                existingPassenger.FirstName = updatePassengerDto.FirstName;
            
            if (!string.IsNullOrEmpty(updatePassengerDto.LastName))
                existingPassenger.LastName = updatePassengerDto.LastName;
            
            if (!string.IsNullOrEmpty(updatePassengerDto.Email))
                existingPassenger.Email = updatePassengerDto.Email;
            
            if (!string.IsNullOrEmpty(updatePassengerDto.PhoneNumber))
                existingPassenger.PhoneNumber = updatePassengerDto.PhoneNumber;
            
            if (updatePassengerDto.DateOfBirth.HasValue)
                existingPassenger.DateOfBirth = updatePassengerDto.DateOfBirth;
            
            if (!string.IsNullOrEmpty(updatePassengerDto.ProfilePictureUrl))
                existingPassenger.ProfilePictureUrl = updatePassengerDto.ProfilePictureUrl;
            
            if (updatePassengerDto.PreferredPaymentMethod.HasValue)
                existingPassenger.PreferredPaymentMethod = updatePassengerDto.PreferredPaymentMethod;
            
            if (updatePassengerDto.HomeAddress != null)
                existingPassenger.HomeAddress = _mapper.Map<Address>(updatePassengerDto.HomeAddress);
            
            if (updatePassengerDto.WorkAddress != null)
                existingPassenger.WorkAddress = _mapper.Map<Address>(updatePassengerDto.WorkAddress);

            existingPassenger.UpdatedAt = DateTime.UtcNow;

            var updatedPassenger = await _passengerRepository.UpdateAsync(existingPassenger);

            _logger.LogInformation("Updated passenger with ID {PassengerId}", id);
            return _mapper.Map<PassengerDto>(updatedPassenger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating passenger with ID {PassengerId}", id);
            throw;
        }
    }

    public async Task<bool> DeletePassengerAsync(string id)
    {
        try
        {
            var passenger = await _passengerRepository.GetByIdAsync(id, id);
            if (passenger == null)
            {
                return false;
            }

            await _passengerRepository.DeleteAsync(id, id);

            _logger.LogInformation("Deleted passenger with ID {PassengerId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting passenger with ID {PassengerId}", id);
            throw;
        }
    }

    public async Task<PassengerProfileDto?> GetPassengerProfileAsync(string id)
    {
        try
        {
            var passenger = await _passengerRepository.GetByIdAsync(id, id);
            if (passenger == null)
            {
                return null;
            }

            var profile = _mapper.Map<PassengerProfileDto>(passenger);
            profile.MemberSince = passenger.CreatedAt;

            return profile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting passenger profile with ID {PassengerId}", id);
            throw;
        }
    }

    public async Task<bool> VerifyPassengerAsync(string id)
    {
        try
        {
            var passenger = await _passengerRepository.GetByIdAsync(id, id);
            if (passenger == null)
            {
                return false;
            }

            passenger.IsVerified = true;
            passenger.VerifiedAt = DateTime.UtcNow;
            passenger.UpdatedAt = DateTime.UtcNow;

            await _passengerRepository.UpdateAsync(passenger);

            _logger.LogInformation("Verified passenger with ID {PassengerId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying passenger with ID {PassengerId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<PassengerDto>> SearchPassengersAsync(string? email, string? phoneNumber)
    {
        try
        {
            var query = "SELECT * FROM c WHERE c.isDeleted = false";
            var parameters = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(email))
            {
                query += " AND c.email = @email";
                parameters.Add("email", email);
            }

            if (!string.IsNullOrEmpty(phoneNumber))
            {
                query += " AND c.phoneNumber = @phoneNumber";
                parameters.Add("phoneNumber", phoneNumber);
            }

            var passengers = await _passengerRepository.QueryAsync(query, parameters, "");
            return _mapper.Map<IEnumerable<PassengerDto>>(passengers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching passengers");
            throw;
        }
    }

    public async Task<PassengerDto?> GetPassengerByEmailAsync(string email)
    {
        try
        {
            var query = "SELECT * FROM c WHERE c.email = @email AND c.isDeleted = false";
            var parameters = new { email };
            
            var passengers = await _passengerRepository.QueryAsync(query, parameters, "");
            var passenger = passengers.FirstOrDefault();
            
            return passenger != null ? _mapper.Map<PassengerDto>(passenger) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting passenger by email {Email}", email);
            throw;
        }
    }

    public async Task<PassengerDto?> GetPassengerByPhoneAsync(string phoneNumber)
    {
        try
        {
            var query = "SELECT * FROM c WHERE c.phoneNumber = @phoneNumber AND c.isDeleted = false";
            var parameters = new { phoneNumber };
            
            var passengers = await _passengerRepository.QueryAsync(query, parameters, "");
            var passenger = passengers.FirstOrDefault();
            
            return passenger != null ? _mapper.Map<PassengerDto>(passenger) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting passenger by phone {PhoneNumber}", phoneNumber);
            throw;
        }
    }

    public async Task UpdatePassengerRatingAsync(string passengerId, decimal newRating)
    {
        try
        {
            var passenger = await _passengerRepository.GetByIdAsync(passengerId, passengerId);
            if (passenger != null)
            {
                passenger.Rating = newRating;
                passenger.UpdatedAt = DateTime.UtcNow;
                await _passengerRepository.UpdateAsync(passenger);
                
                _logger.LogInformation("Updated rating for passenger {PassengerId} to {Rating}", passengerId, newRating);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating rating for passenger {PassengerId}", passengerId);
            throw;
        }
    }

    public async Task IncrementTotalRidesAsync(string passengerId)
    {
        try
        {
            var passenger = await _passengerRepository.GetByIdAsync(passengerId, passengerId);
            if (passenger != null)
            {
                passenger.TotalRides++;
                passenger.UpdatedAt = DateTime.UtcNow;
                await _passengerRepository.UpdateAsync(passenger);
                
                _logger.LogInformation("Incremented total rides for passenger {PassengerId} to {TotalRides}", passengerId, passenger.TotalRides);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing total rides for passenger {PassengerId}", passengerId);
            throw;
        }
    }
}


