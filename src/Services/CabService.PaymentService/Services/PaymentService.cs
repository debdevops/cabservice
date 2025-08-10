using AutoMapper;
using CabService.PaymentService.DTOs;
using CabService.Shared.Infrastructure;
using CabService.Shared.Models;
using CabService.Shared.Events;
using Microsoft.AspNetCore.Http;

namespace CabService.PaymentService.Services;

public class PaymentService : IPaymentService
{
    private readonly ICosmosRepository<Payment> _paymentRepository;
    private readonly IServiceBusPublisher _serviceBusPublisher;
    private readonly IPaymentGatewayService _paymentGatewayService;
    private readonly IMapper _mapper;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        ICosmosRepository<Payment> paymentRepository,
        IServiceBusPublisher serviceBusPublisher,
        IPaymentGatewayService paymentGatewayService,
        IMapper mapper,
        ILogger<PaymentService> logger)
    {
        _paymentRepository = paymentRepository;
        _serviceBusPublisher = serviceBusPublisher;
        _paymentGatewayService = paymentGatewayService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PaymentDto> ProcessPaymentAsync(ProcessPaymentDto processPaymentDto)
    {
        try
        {
            var payment = new Payment
            {
                Id = Guid.NewGuid().ToString(),
                BookingId = processPaymentDto.BookingId,
                PassengerId = processPaymentDto.PassengerId,
                DriverId = processPaymentDto.DriverId,
                Amount = processPaymentDto.Amount,
                TipAmount = processPaymentDto.TipAmount,
                PaymentMethod = processPaymentDto.PaymentMethod,
                Status = PaymentStatus.Pending,
                TransactionId = Guid.NewGuid().ToString(),
                Breakdown = _mapper.Map<PaymentBreakdown>(processPaymentDto.Breakdown) ?? new PaymentBreakdown(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdPayment = await _paymentRepository.CreateAsync(payment);

            // Publish payment initiated event
            var paymentInitiatedEvent = new PaymentInitiatedEvent
            {
                PaymentId = createdPayment.Id,
                BookingId = createdPayment.BookingId,
                PassengerId = createdPayment.PassengerId,
                DriverId = createdPayment.DriverId,
                Amount = createdPayment.Amount,
                TipAmount = createdPayment.TipAmount,
                PaymentMethod = createdPayment.PaymentMethod,
                Source = "PaymentService",
                UserId = createdPayment.PassengerId,
                CorrelationId = Guid.NewGuid().ToString()
            };

            await _serviceBusPublisher.PublishAsync(paymentInitiatedEvent, "payment-events");

            // Process payment with gateway
            _ = Task.Run(async () => await ProcessPaymentWithGatewayAsync(createdPayment.Id, processPaymentDto.PaymentToken));

            _logger.LogInformation("Created payment with ID {PaymentId} for booking {BookingId}", 
                createdPayment.Id, createdPayment.BookingId);

            return _mapper.Map<PaymentDto>(createdPayment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment for booking {BookingId}", processPaymentDto.BookingId);
            throw;
        }
    }

    public async Task<PaymentDto?> GetPaymentByIdAsync(string id)
    {
        try
        {
            var payment = await _paymentRepository.GetByIdAsync(id, id);
            return payment != null ? _mapper.Map<PaymentDto>(payment) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment with ID {PaymentId}", id);
            throw;
        }
    }

    public async Task<PaymentDto?> GetPaymentByBookingIdAsync(string bookingId)
    {
        try
        {
            var query = "SELECT * FROM c WHERE c.bookingId = @bookingId AND c.isDeleted = false";
            var parameters = new { bookingId };
            
            var payments = await _paymentRepository.QueryAsync(query, parameters, bookingId);
            var payment = payments.FirstOrDefault();
            
            return payment != null ? _mapper.Map<PaymentDto>(payment) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment for booking {BookingId}", bookingId);
            throw;
        }
    }

    public async Task<PaymentDto?> RetryPaymentAsync(string id)
    {
        try
        {
            var payment = await _paymentRepository.GetByIdAsync(id, id);
            if (payment == null || payment.Status != PaymentStatus.Failed || payment.RetryCount >= 3)
            {
                return null;
            }

            payment.Status = PaymentStatus.Pending;
            payment.RetryCount++;
            payment.UpdatedAt = DateTime.UtcNow;

            await _paymentRepository.UpdateAsync(payment);

            // Retry payment processing
            _ = Task.Run(async () => await ProcessPaymentWithGatewayAsync(payment.Id, null));

            _logger.LogInformation("Retrying payment {PaymentId}, attempt {RetryCount}", id, payment.RetryCount);
            return _mapper.Map<PaymentDto>(payment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying payment with ID {PaymentId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<PaymentDto>> GetPassengerPaymentsAsync(string passengerId)
    {
        try
        {
            var query = "SELECT * FROM c WHERE c.passengerId = @passengerId AND c.isDeleted = false ORDER BY c.createdAt DESC";
            var parameters = new { passengerId };
            
            var payments = await _paymentRepository.QueryAsync(query, parameters, passengerId);
            return _mapper.Map<IEnumerable<PaymentDto>>(payments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payments for passenger {PassengerId}", passengerId);
            throw;
        }
    }

    public async Task<IEnumerable<PaymentDto>> GetDriverPaymentsAsync(string driverId)
    {
        try
        {
            var query = "SELECT * FROM c WHERE c.driverId = @driverId AND c.isDeleted = false ORDER BY c.createdAt DESC";
            var parameters = new { driverId };
            
            var payments = await _paymentRepository.QueryAsync(query, parameters, driverId);
            return _mapper.Map<IEnumerable<PaymentDto>>(payments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payments for driver {DriverId}", driverId);
            throw;
        }
    }

    public async Task HandlePaymentWebhookAsync(string provider, string payload, IHeaderDictionary headers)
    {
        try
        {
            // Validate webhook signature
            var signature = headers["Stripe-Signature"].FirstOrDefault();
            if (string.IsNullOrEmpty(signature) || !await _paymentGatewayService.ValidateWebhookAsync(payload, signature))
            {
                _logger.LogWarning("Invalid webhook signature from provider {Provider}", provider);
                return;
            }

            var webhookData = await _paymentGatewayService.ParseWebhookAsync(payload);
            
            // Find payment by transaction ID
            var query = "SELECT * FROM c WHERE c.paymentGatewayTransactionId = @transactionId AND c.isDeleted = false";
            var parameters = new { transactionId = webhookData.TransactionId };
            
            var payments = await _paymentRepository.QueryAsync(query, parameters, "");
            var payment = payments.FirstOrDefault();

            if (payment != null)
            {
                await UpdatePaymentFromWebhookAsync(payment, webhookData);
            }

            _logger.LogInformation("Processed webhook from {Provider} for transaction {TransactionId}", 
                provider, webhookData.TransactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling payment webhook from provider {Provider}", provider);
            throw;
        }
    }

    public async Task<PaymentStatusDto?> GetPaymentStatusAsync(string id)
    {
        try
        {
            var payment = await _paymentRepository.GetByIdAsync(id, id);
            if (payment == null)
            {
                return null;
            }

            return new PaymentStatusDto
            {
                Id = payment.Id,
                BookingId = payment.BookingId,
                Status = payment.Status,
                Amount = payment.Amount,
                TipAmount = payment.TipAmount,
                ProcessedAt = payment.ProcessedAt,
                FailureReason = payment.FailureReason,
                RetryCount = payment.RetryCount,
                CanRetry = payment.Status == PaymentStatus.Failed && payment.RetryCount < 3,
                CanRefund = payment.Status == PaymentStatus.Completed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment status for {PaymentId}", id);
            throw;
        }
    }

    public async Task<bool> UpdatePaymentStatusAsync(string id, PaymentStatus status, string? failureReason = null)
    {
        try
        {
            var payment = await _paymentRepository.GetByIdAsync(id, id);
            if (payment == null)
            {
                return false;
            }

            payment.Status = status;
            payment.FailureReason = failureReason;
            payment.UpdatedAt = DateTime.UtcNow;

            if (status == PaymentStatus.Completed)
            {
                payment.ProcessedAt = DateTime.UtcNow;
            }

            await _paymentRepository.UpdateAsync(payment);

            _logger.LogInformation("Updated payment {PaymentId} status to {Status}", id, status);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment status for {PaymentId}", id);
            throw;
        }
    }

    private async Task ProcessPaymentWithGatewayAsync(string paymentId, string? paymentToken)
    {
        try
        {
            var payment = await _paymentRepository.GetByIdAsync(paymentId, paymentId);
            if (payment == null)
            {
                return;
            }

            payment.Status = PaymentStatus.Processing;
            await _paymentRepository.UpdateAsync(payment);

            var request = new PaymentGatewayRequest
            {
                PaymentToken = paymentToken ?? string.Empty,
                Amount = payment.TotalAmount,
                Currency = "USD",
                Description = $"Cab ride payment for booking {payment.BookingId}",
                PaymentMethod = payment.PaymentMethod,
                Metadata = new Dictionary<string, string>
                {
                    { "PaymentId", payment.Id },
                    { "BookingId", payment.BookingId },
                    { "PassengerId", payment.PassengerId }
                }
            };

            var response = await _paymentGatewayService.ProcessPaymentAsync(request);

            payment.PaymentGatewayTransactionId = response.TransactionId;
            payment.Status = response.Status;
            payment.FailureReason = response.FailureReason;
            
            if (response.IsSuccess)
            {
                payment.ProcessedAt = DateTime.UtcNow;
                
                // Publish payment processed event
                var paymentProcessedEvent = new PaymentProcessedEvent
                {
                    PaymentId = payment.Id,
                    BookingId = payment.BookingId,
                    PassengerId = payment.PassengerId,
                    DriverId = payment.DriverId,
                    Amount = payment.TotalAmount,
                    TransactionId = payment.TransactionId,
                    Status = payment.Status,
                    ProcessedAt = payment.ProcessedAt.Value,
                    Source = "PaymentService",
                    UserId = payment.PassengerId,
                    CorrelationId = Guid.NewGuid().ToString()
                };

                await _serviceBusPublisher.PublishAsync(paymentProcessedEvent, "payment-events");
            }
            else
            {
                // Publish payment failed event
                var paymentFailedEvent = new PaymentFailedEvent
                {
                    PaymentId = payment.Id,
                    BookingId = payment.BookingId,
                    PassengerId = payment.PassengerId,
                    DriverId = payment.DriverId,
                    Amount = payment.TotalAmount,
                    FailureReason = payment.FailureReason ?? "Unknown error",
                    RetryCount = payment.RetryCount,
                    Source = "PaymentService",
                    UserId = payment.PassengerId,
                    CorrelationId = Guid.NewGuid().ToString()
                };

                await _serviceBusPublisher.PublishAsync(paymentFailedEvent, "payment-events");
            }

            await _paymentRepository.UpdateAsync(payment);

            _logger.LogInformation("Processed payment {PaymentId} with gateway, status: {Status}", 
                paymentId, payment.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment {PaymentId} with gateway", paymentId);
            
            // Update payment status to failed
            await UpdatePaymentStatusAsync(paymentId, PaymentStatus.Failed, ex.Message);
        }
    }

    private async Task UpdatePaymentFromWebhookAsync(Payment payment, PaymentWebhookDto webhookData)
    {
        payment.Status = webhookData.Status;
        payment.FailureReason = webhookData.FailureReason;
        payment.UpdatedAt = DateTime.UtcNow;

        if (webhookData.Status == PaymentStatus.Completed && !payment.ProcessedAt.HasValue)
        {
            payment.ProcessedAt = DateTime.UtcNow;
        }

        await _paymentRepository.UpdateAsync(payment);

        // Publish appropriate event based on status
        if (webhookData.Status == PaymentStatus.Completed)
        {
            var paymentProcessedEvent = new PaymentProcessedEvent
            {
                PaymentId = payment.Id,
                BookingId = payment.BookingId,
                PassengerId = payment.PassengerId,
                DriverId = payment.DriverId,
                Amount = payment.TotalAmount,
                TransactionId = payment.TransactionId,
                Status = payment.Status,
                ProcessedAt = payment.ProcessedAt!.Value,
                Source = "PaymentService",
                UserId = payment.PassengerId,
                CorrelationId = Guid.NewGuid().ToString()
            };

            await _serviceBusPublisher.PublishAsync(paymentProcessedEvent, "payment-events");
        }
        else if (webhookData.Status == PaymentStatus.Failed)
        {
            var paymentFailedEvent = new PaymentFailedEvent
            {
                PaymentId = payment.Id,
                BookingId = payment.BookingId,
                PassengerId = payment.PassengerId,
                DriverId = payment.DriverId,
                Amount = payment.TotalAmount,
                FailureReason = payment.FailureReason ?? "Unknown error",
                RetryCount = payment.RetryCount,
                Source = "PaymentService",
                UserId = payment.PassengerId,
                CorrelationId = Guid.NewGuid().ToString()
            };

            await _serviceBusPublisher.PublishAsync(paymentFailedEvent, "payment-events");
        }
    }
}
