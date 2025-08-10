using System.ComponentModel.DataAnnotations;
using CabService.Shared.Models;

namespace CabService.PaymentService.DTOs;

public class PaymentDto
{
    public string Id { get; set; } = string.Empty;
    public string BookingId { get; set; } = string.Empty;
    public string PassengerId { get; set; } = string.Empty;
    public string DriverId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal? TipAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus Status { get; set; }
    public string? TransactionId { get; set; }
    public string? PaymentGatewayTransactionId { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? FailureReason { get; set; }
    public int RetryCount { get; set; }
    public PaymentBreakdownDto Breakdown { get; set; } = new();
    public RefundInfoDto? RefundInfo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ProcessPaymentDto
{
    [Required]
    public string BookingId { get; set; } = string.Empty;
    
    [Required]
    public string PassengerId { get; set; } = string.Empty;
    
    [Required]
    public string DriverId { get; set; } = string.Empty;
    
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }
    
    public decimal? TipAmount { get; set; }
    
    [Required]
    public PaymentMethod PaymentMethod { get; set; }
    
    public string? PaymentToken { get; set; }
    
    public PaymentBreakdownDto? Breakdown { get; set; }
}

public class ProcessRefundDto
{
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal RefundAmount { get; set; }
    
    [Required]
    public string Reason { get; set; } = string.Empty;
}

public class RefundDto
{
    public string Id { get; set; } = string.Empty;
    public string PaymentId { get; set; } = string.Empty;
    public decimal RefundAmount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime RefundedAt { get; set; }
    public string? RefundTransactionId { get; set; }
    public RefundStatus Status { get; set; }
}

public class PaymentStatusDto
{
    public string Id { get; set; } = string.Empty;
    public string BookingId { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }
    public decimal Amount { get; set; }
    public decimal? TipAmount { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? FailureReason { get; set; }
    public int RetryCount { get; set; }
    public bool CanRetry { get; set; }
    public bool CanRefund { get; set; }
}

public class PaymentBreakdownDto
{
    public decimal BaseFare { get; set; }
    public decimal DistanceFare { get; set; }
    public decimal TimeFare { get; set; }
    public decimal SurgePricing { get; set; }
    public decimal Tax { get; set; }
    public decimal ServiceFee { get; set; }
    public decimal Discount { get; set; }
}

public class RefundInfoDto
{
    public decimal RefundAmount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime RefundedAt { get; set; }
    public string? RefundTransactionId { get; set; }
    public RefundStatus Status { get; set; }
}

public class PaymentWebhookDto
{
    public string Provider { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }
    public decimal Amount { get; set; }
    public string? FailureReason { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}
