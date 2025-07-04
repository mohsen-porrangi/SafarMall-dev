using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace BuildingBlocks.Middleware;

public class CurrentUserMiddleware
{
    private readonly RequestDelegate _next;

    public CurrentUserMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var claims = context.User.Claims;

            SetIfExists(context, "UserId", claims, tryParseGuid: true);
            SetIfExists(context, "MasterIdentityId", claims, tryParseGuid: true);
            SetIfExists(context, ClaimTypes.Name, claims);
            SetIfExists(context, ClaimTypes.MobilePhone, claims);
            SetIfExists(context, "NationalCode", claims);
        }

        await _next(context);
    }

    private static void SetIfExists(HttpContext context, string key, IEnumerable<Claim> claims, bool tryParseGuid = false)
    {
        var claim = claims.FirstOrDefault(c => c.Type == key);
        if (claim == null) return;

        if (tryParseGuid && Guid.TryParse(claim.Value, out var guidValue))
        {
            context.Items[key] = guidValue;
        }
        else
        {
            context.Items[key] = claim.Value;
        }
    }
}
