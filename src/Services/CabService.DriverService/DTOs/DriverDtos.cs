using System.ComponentModel.DataAnnotations;
using CabService.Shared.Models;

namespace CabService.DriverService.DTOs;

public class DriverDto
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string LicenseNumber { get; set; } = string.Empty;
    public DateTime LicenseExpiryDate { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public decimal Rating { get; set; }
    public int TotalRides { get; set; }
    public DriverStatus Status { get; set; }
    public LocationDto? CurrentLocation { get; set; }
    public DateTime? LastLocationUpdate { get; set; }
    public VehicleDto? Vehicle { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastOnlineAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateDriverDto
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
    
    [Required]
    public string LicenseNumber { get; set; } = string.Empty;
    
    [Required]
    public DateTime LicenseExpiryDate { get; set; }
    
    [Required]
    public VehicleDto Vehicle { get; set; } = new();
    
    public BankAccountDto? BankAccount { get; set; }
}

public class UpdateDriverDto
{
    [StringLength(100)]
    public string? FirstName { get; set; }
    
    [StringLength(100)]
    public string? LastName { get; set; }
    
    [EmailAddress]
    public string? Email { get; set; }
    
    [Phone]
    public string? PhoneNumber { get; set; }
    
    public DateTime? DateOfBirth { get; set; }
    
    public string? ProfilePictureUrl { get; set; }
    
    public string? LicenseNumber { get; set; }
    
    public DateTime? LicenseExpiryDate { get; set; }
    
    public VehicleDto? Vehicle { get; set; }
    
    public BankAccountDto? BankAccount { get; set; }
}

public class UpdateDriverStatusDto
{
    [Required]
    public DriverStatus Status { get; set; }
}

public class LocationUpdateDto
{
    [Required]
    public double Latitude { get; set; }
    
    [Required]
    public double Longitude { get; set; }
    
    public string? Address { get; set; }
}

public class DriverProfileDto
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public bool IsVerified { get; set; }
    public decimal Rating { get; set; }
    public int TotalRides { get; set; }
    public DateTime DriverSince { get; set; }
    public VehicleDto? Vehicle { get; set; }
    public DriverStatus Status { get; set; }
    public bool IsOnline { get; set; }
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
    public DateTime Timestamp { get; set; }
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

public class BankAccountDto
{
    public string AccountNumber { get; set; } = string.Empty;
    public string RoutingNumber { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
}
