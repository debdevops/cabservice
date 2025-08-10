using AutoMapper;
using CabService.PaymentService.DTOs;
using CabService.Shared.Infrastructure;
using CabService.Shared.Models;
using CabService.Shared.Events;

namespace CabService.PaymentService.Services;

public class RefundService : IRefundService
{
    private readonly ICosmosRepository<Payment> _paymentRepository;
    private readonly IServiceBusPublisher _serviceBusPublisher;
    private readonly IPaymentGatewayService _paymentGatewayService;
    private readonly IMapper _mapper;
    private readonly ILogger<RefundService> _logger;

    public RefundService(
        ICosmosRepository<Payment> paymentRepository,
        IServiceBusPublisher serviceBusPublisher,
        IPaymentGatewayService paymentGatewayService,
        IMapper mapper,
        ILogger<RefundService> logger)
    {
        _paymentRepository = paymentRepository;
        _serviceBusPublisher = serviceBusPublisher;
        _paymentGatewayService = paymentGatewayService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<RefundDto?> ProcessRefundAsync(string paymentId, ProcessRefundDto refundDto)
    {
        try
        {
            var payment = await _paymentRepository.GetByIdAsync(paymentId, paymentId);
            if (payment == null || payment.Status != PaymentStatus.Completed)
            {
                return null;
            }

            // Check if refund amount is valid
            var totalRefunded = payment.RefundInfo?.RefundAmount ?? 0;
            if (totalRefunded + refundDto.RefundAmount > payment.TotalAmount)
            {
                throw new InvalidOperationException("Refund amount exceeds available refund amount");
            }

            // Create refund info
            var refundInfo = new RefundInfo
            {
                RefundAmount = refundDto.RefundAmount,
                Reason = refundDto.Reason,
                RefundedAt = DateTime.UtcNow,
                Status = RefundStatus.Pending
            };

            payment.RefundInfo = refundInfo;
            payment.UpdatedAt = DateTime.UtcNow;

            await _paymentRepository.UpdateAsync(payment);

            // Publish refund initiated event
            var refundInitiatedEvent = new RefundInitiatedEvent
            {
                PaymentId = payment.Id,
                BookingId = payment.BookingId,
                PassengerId = payment.PassengerId,
                RefundAmount = refundDto.RefundAmount,
                Reason = refundDto.Reason,
                Source = "PaymentService",
                UserId = payment.PassengerId,
                CorrelationId = Guid.NewGuid().ToString()
            };

            await _serviceBusPublisher.PublishAsync(refundInitiatedEvent, "payment-events");

            // Process refund with gateway
            _ = Task.Run(async () => await ProcessRefundWithGatewayAsync(paymentId));

            _logger.LogInformation("Initiated refund for payment {PaymentId}, amount: {RefundAmount}", 
                paymentId, refundDto.RefundAmount);

            return new RefundDto
            {
                Id = Guid.NewGuid().ToString(),
                PaymentId = paymentId,
                RefundAmount = refundDto.RefundAmount,
                Reason = refundDto.Reason,
                RefundedAt = refundInfo.RefundedAt,
                Status = refundInfo.Status
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for payment {PaymentId}", paymentId);
            throw;
        }
    }

    public async Task<RefundDto?> GetRefundByIdAsync(string id)
    {
        try
        {
            // In this simplified implementation, refund info is stored within the payment
            // In a more complex system, refunds might have their own collection
            var query = "SELECT * FROM c WHERE c.refundInfo.id = @refundId AND c.isDeleted = false";
            var parameters = new { refundId = id };
            
            var payments = await _paymentRepository.QueryAsync(query, parameters, "");
            var payment = payments.FirstOrDefault();
            
            if (payment?.RefundInfo == null)
            {
                return null;
            }

            return new RefundDto
            {
                Id = id,
                PaymentId = payment.Id,
                RefundAmount = payment.RefundInfo.RefundAmount,
                Reason = payment.RefundInfo.Reason,
                RefundedAt = payment.RefundInfo.RefundedAt,
                RefundTransactionId = payment.RefundInfo.RefundTransactionId,
                Status = payment.RefundInfo.Status
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting refund with ID {RefundId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<RefundDto>> GetRefundsByPaymentIdAsync(string paymentId)
    {
        try
        {
            var payment = await _paymentRepository.GetByIdAsync(paymentId, paymentId);
            if (payment?.RefundInfo == null)
            {
                return Enumerable.Empty<RefundDto>();
            }

            return new List<RefundDto>
            {
                new RefundDto
                {
                    Id = Guid.NewGuid().ToString(),
                    PaymentId = paymentId,
                    RefundAmount = payment.RefundInfo.RefundAmount,
                    Reason = payment.RefundInfo.Reason,
                    RefundedAt = payment.RefundInfo.RefundedAt,
                    RefundTransactionId = payment.RefundInfo.RefundTransactionId,
                    Status = payment.RefundInfo.Status
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting refunds for payment {PaymentId}", paymentId);
            throw;
        }
    }

    public async Task<bool> UpdateRefundStatusAsync(string id, RefundStatus status, string? failureReason = null)
    {
        try
        {
            var query = "SELECT * FROM c WHERE c.refundInfo.id = @refundId AND c.isDeleted = false";
            var parameters = new { refundId = id };
            
            var payments = await _paymentRepository.QueryAsync(query, parameters, "");
            var payment = payments.FirstOrDefault();
            
            if (payment?.RefundInfo == null)
            {
                return false;
            }

            payment.RefundInfo.Status = status;
            payment.UpdatedAt = DateTime.UtcNow;

            await _paymentRepository.UpdateAsync(payment);

            _logger.LogInformation("Updated refund {RefundId} status to {Status}", id, status);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating refund status for {RefundId}", id);
            throw;
        }
    }

    private async Task ProcessRefundWithGatewayAsync(string paymentId)
    {
        try
        {
            var payment = await _paymentRepository.GetByIdAsync(paymentId, paymentId);
            if (payment?.RefundInfo == null || string.IsNullOrEmpty(payment.PaymentGatewayTransactionId))
            {
                return;
            }

            payment.RefundInfo.Status = RefundStatus.Processing;
            await _paymentRepository.UpdateAsync(payment);

            var request = new RefundGatewayRequest
            {
                TransactionId = payment.PaymentGatewayTransactionId,
                RefundAmount = payment.RefundInfo.RefundAmount,
                Reason = payment.RefundInfo.Reason,
                Metadata = new Dictionary<string, string>
                {
                    { "PaymentId", payment.Id },
                    { "BookingId", payment.BookingId }
                }
            };

            var response = await _paymentGatewayService.ProcessRefundAsync(request);

            payment.RefundInfo.RefundTransactionId = response.RefundTransactionId;
            payment.RefundInfo.Status = response.Status;
            
            if (response.IsSuccess)
            {
                // Update payment status
                if (payment.RefundInfo.RefundAmount >= payment.TotalAmount)
                {
                    payment.Status = PaymentStatus.Refunded;
                }
                else
                {
                    payment.Status = PaymentStatus.PartiallyRefunded;
                }

                // Publish refund processed event
                var refundProcessedEvent = new RefundProcessedEvent
                {
                    PaymentId = payment.Id,
                    BookingId = payment.BookingId,
                    PassengerId = payment.PassengerId,
                    RefundAmount = payment.RefundInfo.RefundAmount,
                    RefundTransactionId = payment.RefundInfo.RefundTransactionId ?? string.Empty,
                    Status = payment.RefundInfo.Status,
                    Source = "PaymentService",
                    UserId = payment.PassengerId,
                    CorrelationId = Guid.NewGuid().ToString()
                };

                await _serviceBusPublisher.PublishAsync(refundProcessedEvent, "payment-events");
            }

            await _paymentRepository.UpdateAsync(payment);

            _logger.LogInformation("Processed refund for payment {PaymentId} with gateway, status: {Status}", 
                paymentId, payment.RefundInfo.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for payment {PaymentId} with gateway", paymentId);
            
            // Update refund status to failed
            await UpdateRefundStatusAsync(paymentId, RefundStatus.Failed, ex.Message);
        }
    }
}
