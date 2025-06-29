namespace BuildingBlocks.Contracts;

public interface ILogService
{
    Task LogInfo(string requestId, string message, string? method, string userId, string ipAddress, Dictionary<string, object>? additionalData = null);
    Task LogError(string requestId, string message, string? method, string userId, string ipAddress, string stackTrace, Dictionary<string, object>? additionalData = null);
    Task LogRequest(string requestId, string url, string? method, string userId, string ipAddress, Dictionary<string, string> headers, object body, int responseStatus, long responseTimeMs);
}

