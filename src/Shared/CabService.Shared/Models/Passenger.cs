using System.ComponentModel.DataAnnotations;

namespace CabService.Shared.Models;

public class Passenger : BaseEntity
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
    
    public bool IsVerified { get; set; } = false;
    
    public DateTime? VerifiedAt { get; set; }
    
    public decimal Rating { get; set; } = 5.0m;
    
    public int TotalRides { get; set; } = 0;
    
    public PaymentMethod? PreferredPaymentMethod { get; set; }
    
    public Address? HomeAddress { get; set; }
    
    public Address? WorkAddress { get; set; }
}

public class Address
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public enum PaymentMethod
{
    CreditCard,
    DebitCard,
    DigitalWallet,
    Cash,
    UPI
}
