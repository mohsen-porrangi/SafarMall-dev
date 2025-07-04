using BuildingBlocks.Contracts;
using BuildingBlocks.Exceptions;

namespace PaymentGateway.API.Services;

/// <summary>
/// سرویس کاربر فعلی برای 
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User
            .FindFirst("UserId");

        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            return userId;

        throw new UnauthorizedDomainException("کاربر احراز هویت نشده است");
    }

    public Guid GetCurrentUserAccountId()
    {
        var accountIdClaim = _httpContextAccessor.HttpContext?.User
            .FindFirst("AccountId");

        if (accountIdClaim != null && Guid.TryParse(accountIdClaim.Value, out var accountId))
            return accountId;

        throw new UnauthorizedDomainException("شناسه حساب کاربری یافت نشد");
    }

    public Guid GetCurrentMasterIdentityId()
    {
        var masterIdClaim = _httpContextAccessor.HttpContext?.User
            .FindFirst("MasterIdentityId");

        if (masterIdClaim != null && Guid.TryParse(masterIdClaim.Value, out var masterId))
            return masterId;

        throw new UnauthorizedDomainException("شناسه اصلی کاربر یافت نشد");
    }

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    /// <summary>
    /// دریافت IP کاربر
    /// </summary>
    public string GetUserIpAddress()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return "unknown";

        // بررسی X-Forwarded-For برای Load Balancer
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        // بررسی X-Real-IP برای Nginx
        var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // IP مستقیم
        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    /// <summary>
    /// دریافت User Agent
    /// </summary>
    public string GetUserAgent()
    {
        return _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString() ?? "unknown";
    }
}