using CabService.PaymentService.DTOs;
using CabService.Shared.Models;
using Stripe;
using Newtonsoft.Json;

namespace CabService.PaymentService.Services;

public class StripePaymentGatewayService : IPaymentGatewayService
{
    private readonly ILogger<StripePaymentGatewayService> _logger;
    private readonly IConfiguration _configuration;
    private readonly PaymentIntentService _paymentIntentService;
    private readonly Stripe.RefundService _refundService;

    public StripePaymentGatewayService(ILogger<StripePaymentGatewayService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _paymentIntentService = new PaymentIntentService();
        _refundService = new Stripe.RefundService();
    }

    public async Task<PaymentGatewayResponse> ProcessPaymentAsync(PaymentGatewayRequest request)
    {
        try
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(request.Amount * 100), // Convert to cents
                Currency = request.Currency.ToLower(),
                Description = request.Description,
                PaymentMethod = request.PaymentToken,
                ConfirmationMethod = "manual",
                Confirm = true,
                Metadata = request.Metadata
            };

            var paymentIntent = await _paymentIntentService.CreateAsync(options);

            var status = MapStripeStatusToPaymentStatus(paymentIntent.Status);
            var isSuccess = status == PaymentStatus.Completed;

            var response = new PaymentGatewayResponse
            {
                IsSuccess = isSuccess,
                TransactionId = paymentIntent.Id,
                Status = status,
                Amount = request.Amount,
                ProcessedAt = DateTime.UtcNow,
                RawResponse = new Dictionary<string, object>
                {
                    { "stripe_payment_intent", paymentIntent }
                }
            };

            if (!isSuccess && !string.IsNullOrEmpty(paymentIntent.LastPaymentError?.Message))
            {
                response.FailureReason = paymentIntent.LastPaymentError.Message;
            }

            _logger.LogInformation("Processed Stripe payment {PaymentIntentId} with status {Status}", 
                paymentIntent.Id, paymentIntent.Status);

            return response;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe payment processing failed: {ErrorMessage}", ex.Message);
            
            return new PaymentGatewayResponse
            {
                IsSuccess = false,
                Status = PaymentStatus.Failed,
                FailureReason = ex.Message,
                Amount = request.Amount,
                ProcessedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Payment processing failed with unexpected error");
            
            return new PaymentGatewayResponse
            {
                IsSuccess = false,
                Status = PaymentStatus.Failed,
                FailureReason = "An unexpected error occurred",
                Amount = request.Amount,
                ProcessedAt = DateTime.UtcNow
            };
        }
    }

    public async Task<RefundGatewayResponse> ProcessRefundAsync(RefundGatewayRequest request)
    {
        try
        {
            var options = new RefundCreateOptions
            {
                PaymentIntent = request.TransactionId,
                Amount = (long)(request.RefundAmount * 100), // Convert to cents
                Reason = "requested_by_customer",
                Metadata = request.Metadata
            };

            var refund = await _refundService.CreateAsync(options);

            var status = MapStripeRefundStatusToRefundStatus(refund.Status);
            var isSuccess = status == RefundStatus.Completed;

            var response = new RefundGatewayResponse
            {
                IsSuccess = isSuccess,
                RefundTransactionId = refund.Id,
                Status = status,
                RefundAmount = request.RefundAmount,
                ProcessedAt = DateTime.UtcNow,
                RawResponse = new Dictionary<string, object>
                {
                    { "stripe_refund", refund }
                }
            };

            if (!isSuccess && !string.IsNullOrEmpty(refund.FailureReason))
            {
                response.FailureReason = refund.FailureReason;
            }

            _logger.LogInformation("Processed Stripe refund {RefundId} with status {Status}", 
                refund.Id, refund.Status);

            return response;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe refund processing failed: {ErrorMessage}", ex.Message);
            
            return new RefundGatewayResponse
            {
                IsSuccess = false,
                Status = RefundStatus.Failed,
                FailureReason = ex.Message,
                RefundAmount = request.RefundAmount,
                ProcessedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refund processing failed with unexpected error");
            
            return new RefundGatewayResponse
            {
                IsSuccess = false,
                Status = RefundStatus.Failed,
                FailureReason = "An unexpected error occurred",
                RefundAmount = request.RefundAmount,
                ProcessedAt = DateTime.UtcNow
            };
        }
    }

    public async Task<PaymentGatewayResponse> GetPaymentStatusAsync(string transactionId)
    {
        try
        {
            var paymentIntent = await _paymentIntentService.GetAsync(transactionId);
            
            var status = MapStripeStatusToPaymentStatus(paymentIntent.Status);
            var isSuccess = status == PaymentStatus.Completed;

            return new PaymentGatewayResponse
            {
                IsSuccess = isSuccess,
                TransactionId = paymentIntent.Id,
                Status = status,
                Amount = paymentIntent.Amount / 100m, // Convert from cents
                ProcessedAt = DateTime.UtcNow,
                RawResponse = new Dictionary<string, object>
                {
                    { "stripe_payment_intent", paymentIntent }
                }
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to get Stripe payment status for {TransactionId}", transactionId);
            throw;
        }
    }

    public async Task<bool> ValidateWebhookAsync(string payload, string signature)
    {
        try
        {
            var webhookSecret = _configuration["Stripe:WebhookSecret"];
            if (string.IsNullOrEmpty(webhookSecret))
            {
                _logger.LogWarning("Stripe webhook secret not configured");
                return false;
            }

            var stripeEvent = EventUtility.ConstructEvent(payload, signature, webhookSecret);
            return await Task.FromResult(stripeEvent != null);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe webhook validation failed");
            return false;
        }
    }

    public async Task<PaymentWebhookDto> ParseWebhookAsync(string payload)
    {
        try
        {
            var webhookSecret = _configuration["Stripe:WebhookSecret"];
            var stripeEvent = EventUtility.ParseEvent(payload);

            var webhookDto = new PaymentWebhookDto
            {
                Provider = "stripe",
                EventType = stripeEvent.Type
            };

            if (stripeEvent.Data.Object is PaymentIntent paymentIntent)
            {
                webhookDto.TransactionId = paymentIntent.Id;
                webhookDto.Status = MapStripeStatusToPaymentStatus(paymentIntent.Status);
                webhookDto.Amount = paymentIntent.Amount / 100m; // Convert from cents
                
                if (paymentIntent.LastPaymentError != null)
                {
                    webhookDto.FailureReason = paymentIntent.LastPaymentError.Message;
                }

                webhookDto.Metadata = paymentIntent.Metadata?.ToDictionary(
                    kvp => kvp.Key, 
                    kvp => (object)kvp.Value) ?? new Dictionary<string, object>();
            }

            return await Task.FromResult(webhookDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Stripe webhook payload");
            throw;
        }
    }

    private static PaymentStatus MapStripeStatusToPaymentStatus(string stripeStatus)
    {
        return stripeStatus switch
        {
            "requires_payment_method" => PaymentStatus.Pending,
            "requires_confirmation" => PaymentStatus.Pending,
            "requires_action" => PaymentStatus.Pending,
            "processing" => PaymentStatus.Processing,
            "requires_capture" => PaymentStatus.Processing,
            "succeeded" => PaymentStatus.Completed,
            "canceled" => PaymentStatus.Cancelled,
            _ => PaymentStatus.Failed
        };
    }

    private static RefundStatus MapStripeRefundStatusToRefundStatus(string stripeStatus)
    {
        return stripeStatus switch
        {
            "pending" => RefundStatus.Pending,
            "succeeded" => RefundStatus.Completed,
            "failed" => RefundStatus.Failed,
            _ => RefundStatus.Failed
        };
    }
}
