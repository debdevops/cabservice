using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CabService.BookingService.Services;
using CabService.BookingService.DTOs;
using CabService.Shared.Models;

namespace CabService.BookingService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly IDriverMatchingService _driverMatchingService;
    private readonly IFareCalculationService _fareCalculationService;
    private readonly ILogger<BookingController> _logger;

    public BookingController(
        IBookingService bookingService,
        IDriverMatchingService driverMatchingService,
        IFareCalculationService fareCalculationService,
        ILogger<BookingController> logger)
    {
        _bookingService = bookingService;
        _driverMatchingService = driverMatchingService;
        _fareCalculationService = fareCalculationService;
        _logger = logger;
    }

    [HttpPost]
    [Authorize(Policy = "PassengerPolicy")]
    public async Task<ActionResult<BookingDto>> CreateBooking([FromBody] CreateBookingDto createBookingDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var booking = await _bookingService.CreateBookingAsync(createBookingDto);
            return CreatedAtAction(nameof(GetBooking), new { id = booking.Id }, booking);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating booking");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BookingDto>> GetBooking(string id)
    {
        try
        {
            var booking = await _bookingService.GetBookingByIdAsync(id);
            if (booking == null)
            {
                return NotFound($"Booking with ID {id} not found");
            }
            return Ok(booking);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting booking with ID {BookingId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{id}/cancel")]
    public async Task<ActionResult> CancelBooking(string id, [FromBody] CancelBookingDto cancelDto)
    {
        try
        {
            var success = await _bookingService.CancelBookingAsync(id, cancelDto.Reason, cancelDto.CancelledBy);
            if (!success)
            {
                return NotFound($"Booking with ID {id} not found");
            }

            return Ok(new { message = "Booking cancelled successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling booking with ID {BookingId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{id}/accept")]
    [Authorize(Policy = "DriverPolicy")]
    public async Task<ActionResult> AcceptBooking(string id, [FromBody] AcceptBookingDto acceptDto)
    {
        try
        {
            var success = await _bookingService.AcceptBookingAsync(id, acceptDto.DriverId);
            if (!success)
            {
                return NotFound($"Booking with ID {id} not found or cannot be accepted");
            }

            return Ok(new { message = "Booking accepted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting booking with ID {BookingId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{id}/start")]
    [Authorize(Policy = "DriverPolicy")]
    public async Task<ActionResult> StartTrip(string id, [FromBody] StartTripDto startDto)
    {
        try
        {
            var success = await _bookingService.StartTripAsync(id, startDto.DriverId, startDto.StartLocation);
            if (!success)
            {
                return NotFound($"Booking with ID {id} not found or cannot be started");
            }

            return Ok(new { message = "Trip started successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting trip for booking {BookingId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{id}/complete")]
    [Authorize(Policy = "DriverPolicy")]
    public async Task<ActionResult> CompleteTrip(string id, [FromBody] CompleteTripDto completeDto)
    {
        try
        {
            var success = await _bookingService.CompleteTripAsync(id, completeDto.DriverId, completeDto);
            if (!success)
            {
                return NotFound($"Booking with ID {id} not found or cannot be completed");
            }

            return Ok(new { message = "Trip completed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing trip for booking {BookingId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("passenger/{passengerId}")]
    [Authorize(Policy = "PassengerPolicy")]
    public async Task<ActionResult<IEnumerable<BookingDto>>> GetPassengerBookings(string passengerId)
    {
        try
        {
            var bookings = await _bookingService.GetPassengerBookingsAsync(passengerId);
            return Ok(bookings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bookings for passenger {PassengerId}", passengerId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("driver/{driverId}")]
    [Authorize(Policy = "DriverPolicy")]
    public async Task<ActionResult<IEnumerable<BookingDto>>> GetDriverBookings(string driverId)
    {
        try
        {
            var bookings = await _bookingService.GetDriverBookingsAsync(driverId);
            return Ok(bookings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bookings for driver {DriverId}", driverId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("estimate-fare")]
    public async Task<ActionResult<FareEstimateDto>> EstimateFare([FromBody] FareEstimateRequestDto request)
    {
        try
        {
            var estimate = await _fareCalculationService.CalculateFareAsync(
                request.PickupLocation, 
                request.DropoffLocation, 
                request.VehicleType);
            
            return Ok(estimate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error estimating fare");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("nearby-drivers")]
    public async Task<ActionResult<IEnumerable<NearbyDriverDto>>> GetNearbyDrivers(
        [FromQuery] double latitude, 
        [FromQuery] double longitude, 
        [FromQuery] VehicleType? vehicleType = null)
    {
        try
        {
            var drivers = await _driverMatchingService.FindNearbyDriversAsync(latitude, longitude, vehicleType);
            return Ok(drivers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting nearby drivers");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{id}/rate")]
    public async Task<ActionResult> RateTrip(string id, [FromBody] RateTripDto rateDto)
    {
        try
        {
            var success = await _bookingService.RateTripAsync(id, rateDto);
            if (!success)
            {
                return NotFound($"Booking with ID {id} not found");
            }

            return Ok(new { message = "Trip rated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rating trip for booking {BookingId}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}
