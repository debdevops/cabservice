using System.ComponentModel.DataAnnotations;

namespace CabService.Shared.Models;

public class Driver : BaseEntity
{
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;
    
    public DateTime? DateOfBirth { get; set; }
    
    public string? ProfilePictureUrl { get; set; }
    
    [Required]
    public string LicenseNumber { get; set; } = string.Empty;
    
    public DateTime LicenseExpiryDate { get; set; }
    
    public bool IsVerified { get; set; } = false;
    
    public DateTime? VerifiedAt { get; set; }
    
    public decimal Rating { get; set; } = 5.0m;
    
    public int TotalRides { get; set; } = 0;
    
    public DriverStatus Status { get; set; } = DriverStatus.Offline;
    
    public Location? CurrentLocation { get; set; }
    
    public DateTime? LastLocationUpdate { get; set; }
    
    public Vehicle? Vehicle { get; set; }
    
    public BankAccount? BankAccount { get; set; }
    
    public bool IsOnline { get; set; } = false;
    
    public DateTime? LastOnlineAt { get; set; }
}

public class Vehicle
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

public class Location
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Address { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class BankAccount
{
    public string AccountNumber { get; set; } = string.Empty;
    public string RoutingNumber { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
}

public enum DriverStatus
{
    Offline,
    Available,
    Busy,
    OnTrip,
    Break
}

public enum VehicleType
{
    Sedan,
    SUV,
    Hatchback,
    Luxury,
    Electric,
    Bike,
    Auto
}
