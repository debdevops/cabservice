using CabService.Shared.Models;

namespace CabService.Shared.Events;

public class DriverRegisteredEvent : BaseEvent
{
    public string DriverId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public Vehicle Vehicle { get; set; } = new();
}

public class DriverLocationUpdatedEvent : BaseEvent
{
    public string DriverId { get; set; } = string.Empty;
    public Location CurrentLocation { get; set; } = new();
    public DriverStatus Status { get; set; }
    public bool IsOnline { get; set; }
    
    // Additional properties for backward compatibility
    public double Latitude => CurrentLocation?.Latitude ?? 0;
    public double Longitude => CurrentLocation?.Longitude ?? 0;
    public string Address => CurrentLocation?.Address ?? string.Empty;
}

public class DriverStatusChangedEvent : BaseEvent
{
    public string DriverId { get; set; } = string.Empty;
    public DriverStatus PreviousStatus { get; set; }
    public DriverStatus NewStatus { get; set; }
    public Location? CurrentLocation { get; set; }
}

public class DriverVerifiedEvent : BaseEvent
{
    public string DriverId { get; set; } = string.Empty;
    public DateTime VerifiedAt { get; set; }
    public string VerifiedBy { get; set; } = string.Empty;
}

public class DriverOnlineEvent : BaseEvent
{
    public string DriverId { get; set; } = string.Empty;
    public Location CurrentLocation { get; set; } = new();
    public DateTime OnlineAt { get; set; }
}

public class DriverOfflineEvent : BaseEvent
{
    public string DriverId { get; set; } = string.Empty;
    public Location? LastKnownLocation { get; set; }
    public DateTime OfflineAt { get; set; }
}
