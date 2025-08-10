using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using CabService.Shared.Infrastructure;
using CabService.Shared.Models;
using CabService.Shared.Events;

namespace CabService.Functions.Functions;

public class BookingTimeoutFunction
{
    private readonly ILogger<BookingTimeoutFunction> _logger;
    private readonly ICosmosRepository<Booking> _bookingRepository;
    private readonly IServiceBusPublisher _serviceBusPublisher;

    public BookingTimeoutFunction(
        ILogger<BookingTimeoutFunction> logger,
        ICosmosRepository<Booking> bookingRepository,
        IServiceBusPublisher serviceBusPublisher)
    {
        _logger = logger;
        _bookingRepository = bookingRepository;
        _serviceBusPublisher = serviceBusPublisher;
    }

    [Function("ProcessBookingTimeouts")]
    public async Task ProcessBookingTimeouts(
        [TimerTrigger("0 */2 * * * *")] TimerInfo timer) // Every 2 minutes
    {
        try
        {
            var timeoutThreshold = DateTime.UtcNow.AddMinutes(-15); // 15 minutes timeout
            var query = "SELECT * FROM c WHERE c.status = @status AND c.createdAt < @threshold";
            var parameters = new { status = BookingStatus.Requested.ToString(), threshold = timeoutThreshold };

            var timedOutBookings = await _bookingRepository.QueryAsync(query, parameters, "");

            foreach (var booking in timedOutBookings)
            {
                booking.Status = BookingStatus.Cancelled;
                booking.CancellationReason = "Booking timeout - no driver found";
                booking.CancelledAt = DateTime.UtcNow;
                booking.UpdatedAt = DateTime.UtcNow;

                await _bookingRepository.UpdateAsync(booking);

                // Publish booking cancelled event
                var cancelledEvent = new BookingCancelledEvent
                {
                    BookingId = booking.Id,
                    PassengerId = booking.PassengerId,
                    DriverId = booking.DriverId,
                    CancelledBy = "system",
                    CancellationReason = "Booking timeout",
                    Timestamp = DateTime.UtcNow
                };

                await _serviceBusPublisher.PublishAsync(cancelledEvent, "booking-events");

                _logger.LogInformation("Cancelled booking {BookingId} due to timeout", booking.Id);
            }

            if (timedOutBookings.Any())
            {
                _logger.LogInformation("Processed {Count} timed out bookings", timedOutBookings.Count());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing booking timeouts");
        }
    }

    [Function("ProcessExpiredBookings")]
    public async Task ProcessExpiredBookings(
        [TimerTrigger("0 0 2 * * *")] TimerInfo timer) // Daily at 2 AM
    {
        try
        {
            var expiredThreshold = DateTime.UtcNow.AddDays(-30); // 30 days old
            var query = "SELECT * FROM c WHERE c.status IN (@completed, @cancelled) AND c.updatedAt < @threshold";
            var parameters = new { 
                completed = BookingStatus.Completed.ToString(), 
                cancelled = BookingStatus.Cancelled.ToString(),
                threshold = expiredThreshold 
            };

            var expiredBookings = await _bookingRepository.QueryAsync(query, parameters, "");

            foreach (var booking in expiredBookings)
            {
                booking.IsDeleted = true;
                booking.UpdatedAt = DateTime.UtcNow;
                await _bookingRepository.UpdateAsync(booking);
            }

            _logger.LogInformation("Archived {Count} expired bookings", expiredBookings.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing expired bookings");
        }
    }
}
