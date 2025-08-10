using CabService.Shared.Models;

namespace CabService.Shared.Events;

public class BookingRequestedEvent : BaseEvent
{
    public string BookingId { get; set; } = string.Empty;
    public string PassengerId { get; set; } = string.Empty;
    public Location PickupLocation { get; set; } = new();
    public Location DropoffLocation { get; set; } = new();
    public VehicleType RequestedVehicleType { get; set; }
    public decimal EstimatedFare { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? SpecialInstructions { get; set; }
}

public class BookingCreatedEvent : BaseEvent
{
    public string BookingId { get; set; } = string.Empty;
    public string PassengerId { get; set; } = string.Empty;
    public Location PickupLocation { get; set; } = new();
    public Location DropoffLocation { get; set; } = new();
    public VehicleType RequestedVehicleType { get; set; }
    public decimal EstimatedFare { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public BookingStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? SpecialInstructions { get; set; }
}

public class DriverAssignedEvent : BaseEvent
{
    public string BookingId { get; set; } = string.Empty;
    public string PassengerId { get; set; } = string.Empty;
    public string DriverId { get; set; } = string.Empty;
    public Location DriverLocation { get; set; } = new();
    public Vehicle DriverVehicle { get; set; } = new();
    public int EstimatedArrivalTime { get; set; } // in minutes
}

public class BookingAcceptedEvent : BaseEvent
{
    public string BookingId { get; set; } = string.Empty;
    public string PassengerId { get; set; } = string.Empty;
    public string DriverId { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public string VehicleDetails { get; set; } = string.Empty;
    public Location PickupLocation { get; set; } = new();
    public Location DriverLocation { get; set; } = new();
    public Vehicle DriverVehicle { get; set; } = new();
    public int EstimatedArrivalTime { get; set; } // in minutes
}

public class DriverEnRouteEvent : BaseEvent
{
    public string BookingId { get; set; } = string.Empty;
    public string PassengerId { get; set; } = string.Empty;
    public string DriverId { get; set; } = string.Empty;
    public Location CurrentLocation { get; set; } = new();
    public int EstimatedArrivalTime { get; set; } // in minutes
}

public class DriverArrivedEvent : BaseEvent
{
    public string BookingId { get; set; } = string.Empty;
    public string PassengerId { get; set; } = string.Empty;
    public string DriverId { get; set; } = string.Empty;
    public Location ArrivalLocation { get; set; } = new();
}

public class TripStartedEvent : BaseEvent
{
    public string BookingId { get; set; } = string.Empty;
    public string PassengerId { get; set; } = string.Empty;
    public string DriverId { get; set; } = string.Empty;
    public Location StartLocation { get; set; } = new();
    public DateTime StartTime { get; set; }
}

public class TripCompletedEvent : BaseEvent
{
    public string BookingId { get; set; } = string.Empty;
    public string PassengerId { get; set; } = string.Empty;
    public string DriverId { get; set; } = string.Empty;
    public Location EndLocation { get; set; } = new();
    public DateTime EndTime { get; set; }
    public double ActualDistance { get; set; }
    public int ActualDuration { get; set; }
    public decimal ActualFare { get; set; }
    public decimal TotalFare { get; set; }
    public decimal DriverEarnings { get; set; }
}

public class BookingCancelledEvent : BaseEvent
{
    public string BookingId { get; set; } = string.Empty;
    public string PassengerId { get; set; } = string.Empty;
    public string? DriverId { get; set; }
    public string CancellationReason { get; set; } = string.Empty;
    public string CancelledBy { get; set; } = string.Empty; // Passenger or Driver
    public decimal? CancellationFee { get; set; }
}

public class BookingCompletedEvent : BaseEvent
{
    public string BookingId { get; set; } = string.Empty;
    public string PassengerId { get; set; } = string.Empty;
    public string DriverId { get; set; } = string.Empty;
    public Location StartLocation { get; set; } = new();
    public Location EndLocation { get; set; } = new();
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public double ActualDistance { get; set; }
    public int ActualDuration { get; set; }
    public decimal ActualFare { get; set; }
    public decimal? TipAmount { get; set; }
    public BookingStatus FinalStatus { get; set; }
}
