using AutoMapper;
using CabService.BookingService.DTOs;
using CabService.Shared.Infrastructure;
using CabService.Shared.Models;
using CabService.Shared.Events;

namespace CabService.BookingService.Services;

public class BookingService : IBookingService
{
    private readonly ICosmosRepository<Booking> _bookingRepository;
    private readonly IServiceBusPublisher _serviceBusPublisher;
    private readonly IDriverMatchingService _driverMatchingService;
    private readonly IFareCalculationService _fareCalculationService;
    private readonly IMapper _mapper;
    private readonly ILogger<BookingService> _logger;

    public BookingService(
        ICosmosRepository<Booking> bookingRepository,
        IServiceBusPublisher serviceBusPublisher,
        IDriverMatchingService driverMatchingService,
        IFareCalculationService fareCalculationService,
        IMapper mapper,
        ILogger<BookingService> logger)
    {
        _bookingRepository = bookingRepository;
        _serviceBusPublisher = serviceBusPublisher;
        _driverMatchingService = driverMatchingService;
        _fareCalculationService = fareCalculationService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<BookingDto> CreateBookingAsync(CreateBookingDto createBookingDto)
    {
        try
        {
            // Calculate fare estimate
            var fareEstimate = await _fareCalculationService.CalculateFareAsync(
                createBookingDto.PickupLocation,
                createBookingDto.DropoffLocation,
                createBookingDto.RequestedVehicleType);

            var booking = new Booking
            {
                Id = Guid.NewGuid().ToString(),
                PassengerId = createBookingDto.PassengerId,
                PickupLocation = _mapper.Map<Location>(createBookingDto.PickupLocation),
                DropoffLocation = _mapper.Map<Location>(createBookingDto.DropoffLocation),
                RequestedVehicleType = createBookingDto.RequestedVehicleType,
                PaymentMethod = createBookingDto.PaymentMethod,
                SpecialInstructions = createBookingDto.SpecialInstructions,
                EstimatedFare = fareEstimate.TotalEstimatedFare,
                EstimatedDistance = fareEstimate.EstimatedDistance,
                EstimatedDuration = fareEstimate.EstimatedDuration,
                Status = BookingStatus.Requested,
                RequestedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdBooking = await _bookingRepository.CreateAsync(booking);

            // Publish booking requested event
            var bookingRequestedEvent = new BookingRequestedEvent
            {
                BookingId = createdBooking.Id,
                PassengerId = createdBooking.PassengerId,
                PickupLocation = createdBooking.PickupLocation,
                DropoffLocation = createdBooking.DropoffLocation,
                RequestedVehicleType = createdBooking.RequestedVehicleType,
                EstimatedFare = createdBooking.EstimatedFare,
                PaymentMethod = createdBooking.PaymentMethod,
                SpecialInstructions = createdBooking.SpecialInstructions,
                Source = "BookingService",
                UserId = createdBooking.PassengerId,
                CorrelationId = Guid.NewGuid().ToString()
            };

            await _serviceBusPublisher.PublishAsync(bookingRequestedEvent, "booking-events");

            // Try to find and assign a driver
            _ = Task.Run(async () => await TryAssignDriverAsync(createdBooking.Id));

            _logger.LogInformation("Created booking with ID {BookingId} for passenger {PassengerId}", 
                createdBooking.Id, createdBooking.PassengerId);

            return _mapper.Map<BookingDto>(createdBooking);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating booking for passenger {PassengerId}", createBookingDto.PassengerId);
            throw;
        }
    }

    public async Task<BookingDto?> GetBookingByIdAsync(string id)
    {
        try
        {
            var booking = await _bookingRepository.GetByIdAsync(id, id);
            return booking != null ? _mapper.Map<BookingDto>(booking) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting booking with ID {BookingId}", id);
            throw;
        }
    }

    public async Task<bool> CancelBookingAsync(string id, string reason, string cancelledBy)
    {
        try
        {
            var booking = await _bookingRepository.GetByIdAsync(id, id);
            if (booking == null || booking.Status == BookingStatus.Completed || booking.Status == BookingStatus.Cancelled)
            {
                return false;
            }

            booking.Status = BookingStatus.Cancelled;
            booking.CancellationReason = reason;
            booking.UpdatedAt = DateTime.UtcNow;

            await _bookingRepository.UpdateAsync(booking);

            // Publish booking cancelled event
            var bookingCancelledEvent = new BookingCancelledEvent
            {
                BookingId = booking.Id,
                PassengerId = booking.PassengerId,
                DriverId = booking.DriverId,
                CancellationReason = reason,
                CancelledBy = cancelledBy,
                Source = "BookingService",
                UserId = booking.PassengerId,
                CorrelationId = Guid.NewGuid().ToString()
            };

            await _serviceBusPublisher.PublishAsync(bookingCancelledEvent, "booking-events");

            _logger.LogInformation("Cancelled booking {BookingId} by {CancelledBy}", id, cancelledBy);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling booking with ID {BookingId}", id);
            throw;
        }
    }

    public async Task<bool> AcceptBookingAsync(string id, string driverId)
    {
        try
        {
            var booking = await _bookingRepository.GetByIdAsync(id, id);
            if (booking == null || booking.Status != BookingStatus.Requested)
            {
                return false;
            }

            booking.DriverId = driverId;
            booking.Status = BookingStatus.DriverAssigned;
            booking.AcceptedAt = DateTime.UtcNow;
            booking.UpdatedAt = DateTime.UtcNow;

            await _bookingRepository.UpdateAsync(booking);

            // Publish driver assigned event
            var driverAssignedEvent = new DriverAssignedEvent
            {
                BookingId = booking.Id,
                PassengerId = booking.PassengerId,
                DriverId = driverId,
                Source = "BookingService",
                UserId = booking.PassengerId,
                CorrelationId = Guid.NewGuid().ToString()
            };

            await _serviceBusPublisher.PublishAsync(driverAssignedEvent, "booking-events");

            _logger.LogInformation("Driver {DriverId} accepted booking {BookingId}", driverId, id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting booking {BookingId} by driver {DriverId}", id, driverId);
            throw;
        }
    }

    public async Task<bool> StartTripAsync(string id, string driverId, LocationDto startLocation)
    {
        try
        {
            var booking = await _bookingRepository.GetByIdAsync(id, id);
            if (booking == null || booking.DriverId != driverId || booking.Status != BookingStatus.DriverArrived)
            {
                return false;
            }

            booking.Status = BookingStatus.InProgress;
            booking.PickupTime = DateTime.UtcNow;
            booking.UpdatedAt = DateTime.UtcNow;

            await _bookingRepository.UpdateAsync(booking);

            // Publish trip started event
            var tripStartedEvent = new TripStartedEvent
            {
                BookingId = booking.Id,
                PassengerId = booking.PassengerId,
                DriverId = driverId,
                StartLocation = _mapper.Map<Location>(startLocation),
                StartTime = booking.PickupTime.Value,
                Source = "BookingService",
                UserId = booking.PassengerId,
                CorrelationId = Guid.NewGuid().ToString()
            };

            await _serviceBusPublisher.PublishAsync(tripStartedEvent, "booking-events");

            _logger.LogInformation("Started trip for booking {BookingId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting trip for booking {BookingId}", id);
            throw;
        }
    }

    public async Task<bool> CompleteTripAsync(string id, string driverId, CompleteTripDto completeDto)
    {
        try
        {
            var booking = await _bookingRepository.GetByIdAsync(id, id);
            if (booking == null || booking.DriverId != driverId || booking.Status != BookingStatus.InProgress)
            {
                return false;
            }

            booking.Status = BookingStatus.Completed;
            booking.DropoffTime = DateTime.UtcNow;
            booking.ActualDistance = completeDto.ActualDistance;
            booking.ActualDuration = completeDto.ActualDuration;
            booking.ActualFare = completeDto.ActualFare;
            booking.UpdatedAt = DateTime.UtcNow;

            await _bookingRepository.UpdateAsync(booking);

            // Publish trip completed event
            var tripCompletedEvent = new TripCompletedEvent
            {
                BookingId = booking.Id,
                PassengerId = booking.PassengerId,
                DriverId = driverId,
                EndLocation = _mapper.Map<Location>(completeDto.EndLocation),
                EndTime = booking.DropoffTime.Value,
                ActualDistance = completeDto.ActualDistance,
                ActualDuration = completeDto.ActualDuration,
                ActualFare = completeDto.ActualFare,
                Source = "BookingService",
                UserId = booking.PassengerId,
                CorrelationId = Guid.NewGuid().ToString()
            };

            await _serviceBusPublisher.PublishAsync(tripCompletedEvent, "booking-events");

            _logger.LogInformation("Completed trip for booking {BookingId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing trip for booking {BookingId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<BookingDto>> GetPassengerBookingsAsync(string passengerId)
    {
        try
        {
            var query = "SELECT * FROM c WHERE c.passengerId = @passengerId AND c.isDeleted = false ORDER BY c.createdAt DESC";
            var parameters = new { passengerId };
            
            var bookings = await _bookingRepository.QueryAsync(query, parameters, passengerId);
            return _mapper.Map<IEnumerable<BookingDto>>(bookings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bookings for passenger {PassengerId}", passengerId);
            throw;
        }
    }

    public async Task<IEnumerable<BookingDto>> GetDriverBookingsAsync(string driverId)
    {
        try
        {
            var query = "SELECT * FROM c WHERE c.driverId = @driverId AND c.isDeleted = false ORDER BY c.createdAt DESC";
            var parameters = new { driverId };
            
            var bookings = await _bookingRepository.QueryAsync(query, parameters, driverId);
            return _mapper.Map<IEnumerable<BookingDto>>(bookings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bookings for driver {DriverId}", driverId);
            throw;
        }
    }

    public async Task<bool> RateTripAsync(string id, RateTripDto rateDto)
    {
        try
        {
            var booking = await _bookingRepository.GetByIdAsync(id, id);
            if (booking == null || booking.Status != BookingStatus.Completed)
            {
                return false;
            }

            var rating = new Rating
            {
                Score = rateDto.Score,
                Comment = rateDto.Comment,
                RatedAt = DateTime.UtcNow
            };

            if (rateDto.RatedBy == "Passenger")
            {
                booking.PassengerRating = rating;
            }
            else if (rateDto.RatedBy == "Driver")
            {
                booking.DriverRating = rating;
            }

            booking.UpdatedAt = DateTime.UtcNow;
            await _bookingRepository.UpdateAsync(booking);

            _logger.LogInformation("Added rating for booking {BookingId} by {RatedBy}", id, rateDto.RatedBy);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rating trip for booking {BookingId}", id);
            throw;
        }
    }

    public async Task<bool> UpdateBookingStatusAsync(string id, BookingStatus status)
    {
        try
        {
            var booking = await _bookingRepository.GetByIdAsync(id, id);
            if (booking == null)
            {
                return false;
            }

            booking.Status = status;
            booking.UpdatedAt = DateTime.UtcNow;

            await _bookingRepository.UpdateAsync(booking);

            _logger.LogInformation("Updated booking {BookingId} status to {Status}", id, status);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating booking status for {BookingId}", id);
            throw;
        }
    }

    public async Task<BookingDto?> GetActiveBookingForPassengerAsync(string passengerId)
    {
        try
        {
            var query = "SELECT * FROM c WHERE c.passengerId = @passengerId AND c.status IN (@requested, @assigned, @enroute, @arrived, @inprogress) AND c.isDeleted = false";
            var parameters = new { 
                passengerId,
                requested = BookingStatus.Requested.ToString(),
                assigned = BookingStatus.DriverAssigned.ToString(),
                enroute = BookingStatus.DriverEnRoute.ToString(),
                arrived = BookingStatus.DriverArrived.ToString(),
                inprogress = BookingStatus.InProgress.ToString()
            };
            
            var bookings = await _bookingRepository.QueryAsync(query, parameters, passengerId);
            var activeBooking = bookings.FirstOrDefault();
            
            return activeBooking != null ? _mapper.Map<BookingDto>(activeBooking) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active booking for passenger {PassengerId}", passengerId);
            throw;
        }
    }

    public async Task<BookingDto?> GetActiveBookingForDriverAsync(string driverId)
    {
        try
        {
            var query = "SELECT * FROM c WHERE c.driverId = @driverId AND c.status IN (@assigned, @enroute, @arrived, @inprogress) AND c.isDeleted = false";
            var parameters = new { 
                driverId,
                assigned = BookingStatus.DriverAssigned.ToString(),
                enroute = BookingStatus.DriverEnRoute.ToString(),
                arrived = BookingStatus.DriverArrived.ToString(),
                inprogress = BookingStatus.InProgress.ToString()
            };
            
            var bookings = await _bookingRepository.QueryAsync(query, parameters, driverId);
            var activeBooking = bookings.FirstOrDefault();
            
            return activeBooking != null ? _mapper.Map<BookingDto>(activeBooking) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active booking for driver {DriverId}", driverId);
            throw;
        }
    }

    private async Task TryAssignDriverAsync(string bookingId)
    {
        try
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId, bookingId);
            if (booking == null || booking.Status != BookingStatus.Requested)
            {
                return;
            }

            var driverId = await _driverMatchingService.FindBestDriverAsync(
                booking.PickupLocation.Latitude,
                booking.PickupLocation.Longitude,
                booking.RequestedVehicleType);

            if (!string.IsNullOrEmpty(driverId))
            {
                await AcceptBookingAsync(bookingId, driverId);
            }
            else
            {
                // No driver available, update status
                await UpdateBookingStatusAsync(bookingId, BookingStatus.NoDriverAvailable);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error trying to assign driver to booking {BookingId}", bookingId);
        }
    }
}
