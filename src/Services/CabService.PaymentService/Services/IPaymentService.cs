using CabService.PaymentService.DTOs;
using Microsoft.AspNetCore.Http;

namespace CabService.PaymentService.Services;

public interface IPaymentService
{
    Task<PaymentDto> ProcessPaymentAsync(ProcessPaymentDto processPaymentDto);
    Task<PaymentDto?> GetPaymentByIdAsync(string id);
    Task<PaymentDto?> GetPaymentByBookingIdAsync(string bookingId);
    Task<PaymentDto?> RetryPaymentAsync(string id);
    Task<IEnumerable<PaymentDto>> GetPassengerPaymentsAsync(string passengerId);
    Task<IEnumerable<PaymentDto>> GetDriverPaymentsAsync(string driverId);
    Task HandlePaymentWebhookAsync(string provider, string payload, IHeaderDictionary headers);
    Task<PaymentStatusDto?> GetPaymentStatusAsync(string id);
    Task<bool> UpdatePaymentStatusAsync(string id, Shared.Models.PaymentStatus status, string? failureReason = null);
}
