namespace CabService.Shared.Events;


public class DriverWentOnlineEvent : BaseEvent
{
    public string DriverId { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Address { get; set; } = string.Empty;
    public DateTime OnlineAt { get; set; }
}

public class DriverWentOfflineEvent : BaseEvent
{
    public string DriverId { get; set; } = string.Empty;
    public DateTime OfflineAt { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class PassengerRegisteredEvent : BaseEvent
{
    public string PassengerId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}
