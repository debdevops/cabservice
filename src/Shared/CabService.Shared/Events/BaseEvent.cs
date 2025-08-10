using Newtonsoft.Json;

namespace CabService.Shared.Events;

public abstract class BaseEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public string EventType { get; set; } = string.Empty;
    
    public string Source { get; set; } = string.Empty;
    
    public string Version { get; set; } = "1.0";
    
    public string CorrelationId { get; set; } = string.Empty;
    
    public string UserId { get; set; } = string.Empty;
    
    protected BaseEvent()
    {
        EventType = GetType().Name;
    }
    
    public string ToJson()
    {
        return JsonConvert.SerializeObject(this, Formatting.None);
    }
    
    public static T? FromJson<T>(string json) where T : BaseEvent
    {
        return JsonConvert.DeserializeObject<T>(json);
    }
}
