using System.ComponentModel.DataAnnotations;

namespace CabService.Shared.Models;

public class Booking : BaseEntity
{
    [Required]
    public string PassengerId { get; set; } = string.Empty;
    
    public string? DriverId { get; set; }
    
    [Required]
    public Location PickupLocation { get; set; } = new();
    
    [Required]
    public Location DropoffLocation { get; set; } = new();
    
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? AcceptedAt { get; set; }
    
    public DateTime? PickupTime { get; set; }
    
    public DateTime? DropoffTime { get; set; }
    
    public DateTime? CancelledAt { get; set; }
    
    public BookingStatus Status { get; set; } = BookingStatus.Requested;
    
    public VehicleType RequestedVehicleType { get; set; }
    
    public decimal EstimatedFare { get; set; }
    
    public decimal? ActualFare { get; set; }
    
    public double EstimatedDistance { get; set; }
    
    public double? ActualDistance { get; set; }
    
    public int EstimatedDuration { get; set; } // in minutes
    
    public int? ActualDuration { get; set; } // in minutes
    
    public PaymentMethod PaymentMethod { get; set; }
    
    public string? PaymentTransactionId { get; set; }
    
    public string? CancellationReason { get; set; }
    
    public string? SpecialInstructions { get; set; }
    
    public List<Location> Route { get; set; } = new();
    
    public Rating? PassengerRating { get; set; }
    
    public Rating? DriverRating { get; set; }
}

public class Rating
{
    public int Score { get; set; } // 1-5
    public string? Comment { get; set; }
    public DateTime RatedAt { get; set; } = DateTime.UtcNow;
}

public enum BookingStatus
{
    Requested,
    DriverAssigned,
    DriverEnRoute,
    DriverArrived,
    InProgress,
    Completed,
    Cancelled,
    NoDriverAvailable
}
