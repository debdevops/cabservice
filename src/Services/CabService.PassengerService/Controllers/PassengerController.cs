using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CabService.PassengerService.Services;
using CabService.PassengerService.DTOs;
using CabService.Shared.Models;

namespace CabService.PassengerService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PassengerController : ControllerBase
{
    private readonly IPassengerService _passengerService;
    private readonly ILogger<PassengerController> _logger;

    public PassengerController(IPassengerService passengerService, ILogger<PassengerController> logger)
    {
        _passengerService = passengerService;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PassengerDto>> GetPassenger(string id)
    {
        try
        {
            var passenger = await _passengerService.GetPassengerByIdAsync(id);
            if (passenger == null)
            {
                return NotFound($"Passenger with ID {id} not found");
            }
            return Ok(passenger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting passenger with ID {PassengerId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<PassengerDto>> CreatePassenger([FromBody] CreatePassengerDto createPassengerDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var passenger = await _passengerService.CreatePassengerAsync(createPassengerDto);
            return CreatedAtAction(nameof(GetPassenger), new { id = passenger.Id }, passenger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating passenger");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<PassengerDto>> UpdatePassenger(string id, [FromBody] UpdatePassengerDto updatePassengerDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var passenger = await _passengerService.UpdatePassengerAsync(id, updatePassengerDto);
            if (passenger == null)
            {
                return NotFound($"Passenger with ID {id} not found");
            }

            return Ok(passenger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating passenger with ID {PassengerId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeletePassenger(string id)
    {
        try
        {
            var success = await _passengerService.DeletePassengerAsync(id);
            if (!success)
            {
                return NotFound($"Passenger with ID {id} not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting passenger with ID {PassengerId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id}/profile")]
    public async Task<ActionResult<PassengerProfileDto>> GetPassengerProfile(string id)
    {
        try
        {
            var profile = await _passengerService.GetPassengerProfileAsync(id);
            if (profile == null)
            {
                return NotFound($"Passenger profile with ID {id} not found");
            }
            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting passenger profile with ID {PassengerId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("{id}/verify")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult> VerifyPassenger(string id)
    {
        try
        {
            var success = await _passengerService.VerifyPassengerAsync(id);
            if (!success)
            {
                return NotFound($"Passenger with ID {id} not found");
            }

            return Ok(new { message = "Passenger verified successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying passenger with ID {PassengerId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<PassengerDto>>> SearchPassengers([FromQuery] string? email, [FromQuery] string? phoneNumber)
    {
        try
        {
            var passengers = await _passengerService.SearchPassengersAsync(email, phoneNumber);
            return Ok(passengers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching passengers");
            return StatusCode(500, "Internal server error");
        }
    }
}
