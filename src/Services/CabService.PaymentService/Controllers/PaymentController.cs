using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CabService.PaymentService.Services;
using CabService.PaymentService.DTOs;
using CabService.Shared.Models;

namespace CabService.PaymentService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IRefundService _refundService;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        IPaymentService paymentService,
        IRefundService refundService,
        ILogger<PaymentController> logger)
    {
        _paymentService = paymentService;
        _refundService = refundService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<PaymentDto>> ProcessPayment([FromBody] ProcessPaymentDto processPaymentDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var payment = await _paymentService.ProcessPaymentAsync(processPaymentDto);
            return Ok(payment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment for booking {BookingId}", processPaymentDto.BookingId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PaymentDto>> GetPayment(string id)
    {
        try
        {
            var payment = await _paymentService.GetPaymentByIdAsync(id);
            if (payment == null)
            {
                return NotFound($"Payment with ID {id} not found");
            }
            return Ok(payment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment with ID {PaymentId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("booking/{bookingId}")]
    public async Task<ActionResult<PaymentDto>> GetPaymentByBooking(string bookingId)
    {
        try
        {
            var payment = await _paymentService.GetPaymentByBookingIdAsync(bookingId);
            if (payment == null)
            {
                return NotFound($"Payment for booking {bookingId} not found");
            }
            return Ok(payment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment for booking {BookingId}", bookingId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("{id}/retry")]
    public async Task<ActionResult<PaymentDto>> RetryPayment(string id)
    {
        try
        {
            var payment = await _paymentService.RetryPaymentAsync(id);
            if (payment == null)
            {
                return NotFound($"Payment with ID {id} not found or cannot be retried");
            }
            return Ok(payment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying payment with ID {PaymentId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("{id}/refund")]
    public async Task<ActionResult<RefundDto>> ProcessRefund(string id, [FromBody] ProcessRefundDto refundDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var refund = await _refundService.ProcessRefundAsync(id, refundDto);
            if (refund == null)
            {
                return NotFound($"Payment with ID {id} not found or cannot be refunded");
            }
            return Ok(refund);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for payment {PaymentId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("passenger/{passengerId}")]
    [Authorize(Policy = "PassengerPolicy")]
    public async Task<ActionResult<IEnumerable<PaymentDto>>> GetPassengerPayments(string passengerId)
    {
        try
        {
            var payments = await _paymentService.GetPassengerPaymentsAsync(passengerId);
            return Ok(payments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payments for passenger {PassengerId}", passengerId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("driver/{driverId}")]
    [Authorize(Policy = "DriverPolicy")]
    public async Task<ActionResult<IEnumerable<PaymentDto>>> GetDriverPayments(string driverId)
    {
        try
        {
            var payments = await _paymentService.GetDriverPaymentsAsync(driverId);
            return Ok(payments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payments for driver {DriverId}", driverId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("webhook/stripe")]
    [AllowAnonymous]
    public async Task<ActionResult> HandleStripeWebhook()
    {
        try
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            await _paymentService.HandlePaymentWebhookAsync("stripe", json, Request.Headers);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Stripe webhook");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id}/status")]
    public async Task<ActionResult<PaymentStatusDto>> GetPaymentStatus(string id)
    {
        try
        {
            var status = await _paymentService.GetPaymentStatusAsync(id);
            if (status == null)
            {
                return NotFound($"Payment with ID {id} not found");
            }
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment status for {PaymentId}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}
