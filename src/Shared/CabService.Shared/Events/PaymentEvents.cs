using CabService.Shared.Models;

namespace CabService.Shared.Events;

public class PaymentInitiatedEvent : BaseEvent
{
    public string PaymentId { get; set; } = string.Empty;
    public string BookingId { get; set; } = string.Empty;
    public string PassengerId { get; set; } = string.Empty;
    public string DriverId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal? TipAmount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
}

public class PaymentProcessedEvent : BaseEvent
{
    public string PaymentId { get; set; } = string.Empty;
    public string BookingId { get; set; } = string.Empty;
    public string PassengerId { get; set; } = string.Empty;
    public string DriverId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }
    public DateTime ProcessedAt { get; set; }
}

public class PaymentFailedEvent : BaseEvent
{
    public string PaymentId { get; set; } = string.Empty;
    public string BookingId { get; set; } = string.Empty;
    public string PassengerId { get; set; } = string.Empty;
    public string DriverId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string FailureReason { get; set; } = string.Empty;
    public int RetryCount { get; set; }
}

public class RefundInitiatedEvent : BaseEvent
{
    public string PaymentId { get; set; } = string.Empty;
    public string BookingId { get; set; } = string.Empty;
    public string PassengerId { get; set; } = string.Empty;
    public decimal RefundAmount { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class RefundProcessedEvent : BaseEvent
{
    public string PaymentId { get; set; } = string.Empty;
    public string BookingId { get; set; } = string.Empty;
    public string PassengerId { get; set; } = string.Empty;
    public decimal RefundAmount { get; set; }
    public decimal Amount { get; set; }
    public string RefundTransactionId { get; set; } = string.Empty;
    public string RefundId { get; set; } = string.Empty;
    public RefundStatus Status { get; set; }
}
