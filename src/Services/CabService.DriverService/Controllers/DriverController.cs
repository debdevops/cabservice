using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CabService.DriverService.Services;
using CabService.DriverService.DTOs;
using CabService.Shared.Models;

namespace CabService.DriverService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DriverController : ControllerBase
{
    private readonly IDriverService _driverService;
    private readonly ILocationService _locationService;
    private readonly ILogger<DriverController> _logger;

    public DriverController(
        IDriverService driverService, 
        ILocationService locationService,
        ILogger<DriverController> logger)
    {
        _driverService = driverService;
        _locationService = locationService;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DriverDto>> GetDriver(string id)
    {
        try
        {
            var driver = await _driverService.GetDriverByIdAsync(id);
            if (driver == null)
            {
                return NotFound($"Driver with ID {id} not found");
            }
            return Ok(driver);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting driver with ID {DriverId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<DriverDto>> CreateDriver([FromBody] CreateDriverDto createDriverDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var driver = await _driverService.CreateDriverAsync(createDriverDto);
            return CreatedAtAction(nameof(GetDriver), new { id = driver.Id }, driver);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating driver");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "DriverPolicy")]
    public async Task<ActionResult<DriverDto>> UpdateDriver(string id, [FromBody] UpdateDriverDto updateDriverDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var driver = await _driverService.UpdateDriverAsync(id, updateDriverDto);
            if (driver == null)
            {
                return NotFound($"Driver with ID {id} not found");
            }

            return Ok(driver);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating driver with ID {DriverId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult> DeleteDriver(string id)
    {
        try
        {
            var success = await _driverService.DeleteDriverAsync(id);
            if (!success)
            {
                return NotFound($"Driver with ID {id} not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting driver with ID {DriverId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("{id}/verify")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult> VerifyDriver(string id)
    {
        try
        {
            var success = await _driverService.VerifyDriverAsync(id);
            if (!success)
            {
                return NotFound($"Driver with ID {id} not found");
            }

            return Ok(new { message = "Driver verified successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying driver with ID {DriverId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("{id}/status")]
    [Authorize(Policy = "DriverPolicy")]
    public async Task<ActionResult> UpdateDriverStatus(string id, [FromBody] UpdateDriverStatusDto statusDto)
    {
        try
        {
            var success = await _driverService.UpdateDriverStatusAsync(id, statusDto.Status);
            if (!success)
            {
                return NotFound($"Driver with ID {id} not found");
            }

            return Ok(new { message = "Driver status updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating driver status with ID {DriverId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("{id}/location")]
    [Authorize(Policy = "DriverPolicy")]
    public async Task<ActionResult> UpdateLocation(string id, [FromBody] LocationUpdateDto locationDto)
    {
        try
        {
            await _locationService.UpdateDriverLocationAsync(id, locationDto);
            return Ok(new { message = "Location updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating location for driver {DriverId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("nearby")]
    public async Task<ActionResult<IEnumerable<NearbyDriverDto>>> GetNearbyDrivers(
        [FromQuery] double latitude, 
        [FromQuery] double longitude, 
        [FromQuery] double radiusKm = 5.0,
        [FromQuery] VehicleType? vehicleType = null)
    {
        try
        {
            var drivers = await _locationService.GetNearbyDriversAsync(latitude, longitude, radiusKm, vehicleType);
            return Ok(drivers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting nearby drivers");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id}/profile")]
    public async Task<ActionResult<DriverProfileDto>> GetDriverProfile(string id)
    {
        try
        {
            var profile = await _driverService.GetDriverProfileAsync(id);
            if (profile == null)
            {
                return NotFound($"Driver profile with ID {id} not found");
            }
            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting driver profile with ID {DriverId}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}
