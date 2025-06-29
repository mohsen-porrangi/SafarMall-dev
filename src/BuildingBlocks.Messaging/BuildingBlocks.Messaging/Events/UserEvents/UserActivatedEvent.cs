using BuildingBlocks.MessagingEvent.Base;
using Newtonsoft.Json;

public record UserActivatedEvent : IntegrationEvent
{
    [JsonConstructor]
    public UserActivatedEvent(Guid userId, string mobile)
    {
        UserId = userId;
        Mobile = mobile;
        Source = "UserManagement";
    }

    public UserActivatedEvent(Guid userId) : this(userId, string.Empty) { }

    public Guid UserId { get; init; }
    public string Mobile { get; init; } = string.Empty;
}