using System.Security.Claims;

namespace PaymentGateway.API.Middleware;

/// <summary>
/// Middleware برای احراز هویت و استخراج اطلاعات کاربر
/// </summary>
public class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthenticationMiddleware> _logger;

    public AuthenticationMiddleware(RequestDelegate next, ILogger<AuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // استخراج اطلاعات کاربر از JWT token
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null)
            {
                // افزودن UserId به HttpContext.Items برای دسترسی آسان
                context.Items["UserId"] = userIdClaim.Value;

                _logger.LogDebug("User authenticated: {UserId}", userIdClaim.Value);
            }

            // استخراج سایر claims مفید
            var emailClaim = context.User.FindFirst(ClaimTypes.Email);
            if (emailClaim != null)
            {
                context.Items["UserEmail"] = emailClaim.Value;
            }
        }

        await _next(context);
    }
}