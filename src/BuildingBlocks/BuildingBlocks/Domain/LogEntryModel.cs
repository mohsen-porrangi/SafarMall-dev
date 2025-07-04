using MongoDB.Bson;

namespace BuildingBlocks.Domain;

public class LogEntryModel
{
    public ObjectId Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Level { get; set; }
    public string? Message { get; set; }
    public string? Service { get; set; }
    public string? Method { get; set; }
    public string? UserId { get; set; }
    public string? IpAddress { get; set; }
    public string? StackTrace { get; set; }
    public Dictionary<string, object>? AdditionalData { get; set; }
    public string? RequestId { get; set; }
}
