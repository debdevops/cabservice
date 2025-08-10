using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Messaging.ServiceBus;
using CabService.Shared.Events;
using CabService.Shared.Infrastructure;
using CabService.Shared.Models;
using System.Text.Json;

namespace CabService.Functions.Functions;

public class PaymentRetryFunction
{
    private readonly ILogger<PaymentRetryFunction> _logger;
    private readonly ICosmosRepository<Payment> _paymentRepository;
    private readonly IServiceBusPublisher _serviceBusPublisher;
    private readonly HttpClient _httpClient;

    public PaymentRetryFunction(
        ILogger<PaymentRetryFunction> logger,
        ICosmosRepository<Payment> paymentRepository,
        IServiceBusPublisher serviceBusPublisher,
        HttpClient httpClient)
    {
        _logger = logger;
        _paymentRepository = paymentRepository;
        _serviceBusPublisher = serviceBusPublisher;
        _httpClient = httpClient;
    }

    [Function("ProcessFailedPayments")]
    public async Task ProcessFailedPayments(
        [TimerTrigger("0 */10 * * * *")] TimerInfo timer) // Every 10 minutes
    {
        try
        {
            var query = "SELECT * FROM c WHERE c.status = @status AND c.retryCount < @maxRetries AND c.nextRetryAt <= @now";
            var parameters = new { 
                status = PaymentStatus.Failed.ToString(), 
                maxRetries = 3,
                now = DateTime.UtcNow 
            };

            var failedPayments = await _paymentRepository.QueryAsync(query, parameters, "");

            foreach (var payment in failedPayments)
            {
                try
                {
                    // Retry payment processing
                    var retryResult = await RetryPaymentAsync(payment);
                    
                    if (retryResult)
                    {
                        payment.Status = PaymentStatus.Processing;
                        payment.RetryCount++;
                        payment.UpdatedAt = DateTime.UtcNow;
                        
                        await _paymentRepository.UpdateAsync(payment);
                        
                        _logger.LogInformation("Retried payment {PaymentId} successfully", payment.Id);
                    }
                    else
                    {
                        payment.RetryCount++;
                        payment.NextRetryAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, payment.RetryCount) * 5); // Exponential backoff
                        payment.UpdatedAt = DateTime.UtcNow;
                        
                        if (payment.RetryCount >= 3)
                        {
                            payment.Status = PaymentStatus.Failed;
                            payment.FailureReason = "Maximum retry attempts exceeded";
                            
                            // Publish payment failed event
                            var failedEvent = new PaymentProcessedEvent
                            {
                                PaymentId = payment.Id,
                                BookingId = payment.BookingId,
                                UserId = payment.UserId,
                                Amount = payment.Amount,
                                Status = PaymentStatus.Failed,
                                Timestamp = DateTime.UtcNow
                            };
                            
                            await _serviceBusPublisher.PublishAsync(failedEvent, "payment-events");
                        }
                        
                        await _paymentRepository.UpdateAsync(payment);
                        
                        _logger.LogWarning("Payment retry failed for payment {PaymentId}, attempt {RetryCount}", 
                            payment.Id, payment.RetryCount);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrying payment {PaymentId}", payment.Id);
                }
            }

            if (failedPayments.Any())
            {
                _logger.LogInformation("Processed {Count} failed payment retries", failedPayments.Count());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing failed payments");
        }
    }

    private async Task<bool> RetryPaymentAsync(Payment payment)
    {
        try
        {
            // Call Payment Service API to retry payment
            var paymentServiceUrl = Environment.GetEnvironmentVariable("PaymentServiceUrl") ?? "https://localhost:7004";
            var response = await _httpClient.PostAsync($"{paymentServiceUrl}/api/payments/{payment.Id}/retry", null);
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling payment service for retry");
            return false;
        }
    }

    [Function("ProcessPendingRefunds")]
    public async Task ProcessPendingRefunds(
        [TimerTrigger("0 */15 * * * *")] TimerInfo timer) // Every 15 minutes
    {
        try
        {
            var query = "SELECT * FROM c WHERE c.refundStatus = @status AND c.refundRequestedAt IS NOT NULL";
            var parameters = new { status = "Pending" };

            var pendingRefunds = await _paymentRepository.QueryAsync(query, parameters, "");

            foreach (var payment in pendingRefunds)
            {
                try
                {
                    // Process refund
                    var refundServiceUrl = Environment.GetEnvironmentVariable("PaymentServiceUrl") ?? "https://localhost:7004";
                    var response = await _httpClient.PostAsync($"{refundServiceUrl}/api/payments/{payment.Id}/process-refund", null);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Processed refund for payment {PaymentId}", payment.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing refund for payment {PaymentId}", payment.Id);
                }
            }

            if (pendingRefunds.Any())
            {
                _logger.LogInformation("Processed {Count} pending refunds", pendingRefunds.Count());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing pending refunds");
        }
    }
}
