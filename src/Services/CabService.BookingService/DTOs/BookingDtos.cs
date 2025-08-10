using System.ComponentModel.DataAnnotations;
using CabService.Shared.Models;

namespace CabService.BookingService.DTOs;

public class BookingDto
{
    public string Id { get; set; } = string.Empty;
    public string PassengerId { get; set; } = string.Empty;
    public string? DriverId { get; set; }
    public LocationDto PickupLocation { get; set; } = new();
    public LocationDto DropoffLocation { get; set; } = new();
    public DateTime RequestedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? PickupTime { get; set; }
    public DateTime? DropoffTime { get; set; }
    public BookingStatus Status { get; set; }
    public VehicleType RequestedVehicleType { get; set; }
    public decimal EstimatedFare { get; set; }
    public decimal? ActualFare { get; set; }
    public double EstimatedDistance { get; set; }
    public double? ActualDistance { get; set; }
    public int EstimatedDuration { get; set; }
    public int? ActualDuration { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? PaymentTransactionId { get; set; }
    public string? CancellationReason { get; set; }
    public string? SpecialInstructions { get; set; }
    public RatingDto? PassengerRating { get; set; }
    public RatingDto? DriverRating { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateBookingDto
{
    [Required]
    public string PassengerId { get; set; } = string.Empty;
    
    [Required]
    public LocationDto PickupLocation { get; set; } = new();
    
    [Required]
    public LocationDto DropoffLocation { get; set; } = new();
    
    [Required]
    public VehicleType RequestedVehicleType { get; set; }
    
    [Required]
    public PaymentMethod PaymentMethod { get; set; }
    
    public string? SpecialInstructions { get; set; }
}

public class CancelBookingDto
{
    [Required]
    public string Reason { get; set; } = string.Empty;
    
    [Required]
    public string CancelledBy { get; set; } = string.Empty; // "Passenger" or "Driver"
}

public class AcceptBookingDto
{
    [Required]
    public string DriverId { get; set; } = string.Empty;
}

public class StartTripDto
{
    [Required]
    public string DriverId { get; set; } = string.Empty;
    
    [Required]
    public LocationDto StartLocation { get; set; } = new();
}

public class CompleteTripDto
{
    [Required]
    public string DriverId { get; set; } = string.Empty;
    
    [Required]
    public LocationDto EndLocation { get; set; } = new();
    
    [Required]
    public double ActualDistance { get; set; }
    
    [Required]
    public int ActualDuration { get; set; }
    
    [Required]
    public decimal ActualFare { get; set; }
}

public class RateTripDto
{
    [Required]
    public string RatedBy { get; set; } = string.Empty; // "Passenger" or "Driver"
    
    [Required]
    [Range(1, 5)]
    public int Score { get; set; }
    
    public string? Comment { get; set; }
}

public class FareEstimateRequestDto
{
    [Required]
    public LocationDto PickupLocation { get; set; } = new();
    
    [Required]
    public LocationDto DropoffLocation { get; set; } = new();
    
    [Required]
    public VehicleType VehicleType { get; set; }
}

public class FareEstimateDto
{
    public decimal BaseFare { get; set; }
    public decimal DistanceFare { get; set; }
    public decimal TimeFare { get; set; }
    public decimal SurgePricing { get; set; }
    public decimal Tax { get; set; }
    public decimal ServiceFee { get; set; }
    public decimal TotalEstimatedFare { get; set; }
    public double EstimatedDistance { get; set; }
    public int EstimatedDuration { get; set; }
    public string Currency { get; set; } = "USD";
}

public class NearbyDriverDto
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public LocationDto CurrentLocation { get; set; } = new();
    public VehicleDto Vehicle { get; set; } = new();
    public decimal Rating { get; set; }
    public double DistanceKm { get; set; }
    public int EstimatedArrivalMinutes { get; set; }
}

public class LocationDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Address { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class VehicleDto
{
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Color { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
    public VehicleType Type { get; set; }
    public int Capacity { get; set; }
    public string? ImageUrl { get; set; }
}

public class RatingDto
{
    public int Score { get; set; }
    public string? Comment { get; set; }
    public DateTime RatedAt { get; set; }
}
