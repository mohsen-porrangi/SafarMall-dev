using BuildingBlocks.Contracts;
using BuildingBlocks.Extensions;
using BuildingBlocks.Middleware;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
namespace BuildingBlocks.Utils.SafeLog.LogService;

public class SafeLogService
{
    private readonly ILogService _logService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private string UserId =>
        _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value!;

    public SafeLogService(ILogService logService, IHttpContextAccessor httpContextAccessor)
    {
        _logService = logService;
        _httpContextAccessor = httpContextAccessor;
    }

    public void Info(string message, string? method = null, Dictionary<string, object>? additionalData = null)
    {

        _logService.LogInfo(GetRequestId()!, message, method, UserId, GetIpAddress(), additionalData)
                   .FireAndForgetSafeAsync();
    }

    public void Error(string message, string stackTrace, string? method = null, Dictionary<string, object>? additionalData = null)
    {

        _logService.LogError(GetRequestId()!, message, method, UserId, GetIpAddress(), stackTrace, additionalData)
                   .FireAndForgetSafeAsync();
    }

    public void Request(string url, Dictionary<string, string> headers, object body, int responseStatus, long responseTimeMs, string? method = null)
    {

        _logService.LogRequest(GetRequestId()!, url, method, UserId, GetIpAddress(), headers, body, responseStatus, responseTimeMs)
                   .FireAndForgetSafeAsync();
    }

    private string? GetRequestId()
    {
        var context = _httpContextAccessor.HttpContext;
        return RequestIdMiddleware.GetRequestId(context!);
    }
    private string GetIpAddress()
    {
        return _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}

