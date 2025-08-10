using System.ComponentModel.DataAnnotations;

namespace CabService.Shared.Models;

public class Payment : BaseEntity
{
    [Required]
    public string BookingId { get; set; } = string.Empty;
    
    [Required]
    public string PassengerId { get; set; } = string.Empty;
    
    [Required]
    public string DriverId { get; set; } = string.Empty;
    
    [Required]
    public decimal Amount { get; set; }
    
    public decimal? TipAmount { get; set; }
    
    public decimal TotalAmount => Amount + (TipAmount ?? 0);
    
    public PaymentMethod PaymentMethod { get; set; }
    
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    
    public string? TransactionId { get; set; }
    
    public string? PaymentGatewayTransactionId { get; set; }
    
    public DateTime? ProcessedAt { get; set; }
    
    public string? FailureReason { get; set; }
    
    public int RetryCount { get; set; } = 0;
    
    public DateTime? NextRetryAt { get; set; }
    
    public string UserId { get; set; } = string.Empty;
    
    public PaymentBreakdown Breakdown { get; set; } = new();
    
    public RefundInfo? RefundInfo { get; set; }
}

public class PaymentBreakdown
{
    public decimal BaseFare { get; set; }
    public decimal DistanceFare { get; set; }
    public decimal TimeFare { get; set; }
    public decimal SurgePricing { get; set; }
    public decimal Tax { get; set; }
    public decimal ServiceFee { get; set; }
    public decimal Discount { get; set; }
}

public class RefundInfo
{
    public decimal RefundAmount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime RefundedAt { get; set; }
    public string? RefundTransactionId { get; set; }
    public RefundStatus Status { get; set; }
}

public enum PaymentStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Cancelled,
    Refunded,
    PartiallyRefunded
}

public enum RefundStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}
