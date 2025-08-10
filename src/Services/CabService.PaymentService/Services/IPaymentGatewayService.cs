using CabService.PaymentService.DTOs;
using CabService.Shared.Models;

namespace CabService.PaymentService.Services;

public interface IPaymentGatewayService
{
    Task<PaymentGatewayResponse> ProcessPaymentAsync(PaymentGatewayRequest request);
    Task<RefundGatewayResponse> ProcessRefundAsync(RefundGatewayRequest request);
    Task<PaymentGatewayResponse> GetPaymentStatusAsync(string transactionId);
    Task<bool> ValidateWebhookAsync(string payload, string signature);
    Task<PaymentWebhookDto> ParseWebhookAsync(string payload);
}

public class PaymentGatewayRequest
{
    public string PaymentToken { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Description { get; set; } = string.Empty;
    public PaymentMethod PaymentMethod { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class PaymentGatewayResponse
{
    public bool IsSuccess { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }
    public string? FailureReason { get; set; }
    public decimal Amount { get; set; }
    public DateTime ProcessedAt { get; set; }
    public Dictionary<string, object> RawResponse { get; set; } = new();
}

public class RefundGatewayRequest
{
    public string TransactionId { get; set; } = string.Empty;
    public decimal RefundAmount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class RefundGatewayResponse
{
    public bool IsSuccess { get; set; }
    public string RefundTransactionId { get; set; } = string.Empty;
    public RefundStatus Status { get; set; }
    public string? FailureReason { get; set; }
    public decimal RefundAmount { get; set; }
    public DateTime ProcessedAt { get; set; }
    public Dictionary<string, object> RawResponse { get; set; } = new();
}
