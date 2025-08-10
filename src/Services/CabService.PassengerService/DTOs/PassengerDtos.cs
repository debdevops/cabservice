using System.ComponentModel.DataAnnotations;
using CabService.Shared.Models;

namespace CabService.PassengerService.DTOs;

public class PassengerDto
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public decimal Rating { get; set; }
    public int TotalRides { get; set; }
    public PaymentMethod? PreferredPaymentMethod { get; set; }
    public AddressDto? HomeAddress { get; set; }
    public AddressDto? WorkAddress { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreatePassengerDto
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
    
    public PaymentMethod? PreferredPaymentMethod { get; set; }
    
    public AddressDto? HomeAddress { get; set; }
    
    public AddressDto? WorkAddress { get; set; }
}

public class UpdatePassengerDto
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
    
    public PaymentMethod? PreferredPaymentMethod { get; set; }
    
    public AddressDto? HomeAddress { get; set; }
    
    public AddressDto? WorkAddress { get; set; }
}

public class PassengerProfileDto
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
    public DateTime MemberSince { get; set; }
    public AddressDto? HomeAddress { get; set; }
    public AddressDto? WorkAddress { get; set; }
}

public class AddressDto
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
