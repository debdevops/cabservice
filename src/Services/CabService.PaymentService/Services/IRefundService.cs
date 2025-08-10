using CabService.PaymentService.DTOs;

namespace CabService.PaymentService.Services;

public interface IRefundService
{
    Task<RefundDto?> ProcessRefundAsync(string paymentId, ProcessRefundDto refundDto);
    Task<RefundDto?> GetRefundByIdAsync(string id);
    Task<IEnumerable<RefundDto>> GetRefundsByPaymentIdAsync(string paymentId);
    Task<bool> UpdateRefundStatusAsync(string id, Shared.Models.RefundStatus status, string? failureReason = null);
}
