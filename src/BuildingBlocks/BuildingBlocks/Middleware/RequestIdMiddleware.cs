using BuildingBlocks.Extensions;
using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Middleware;

public class RequestIdMiddleware
{
    private const string RequestIdKey = "RequestId";
    private readonly RequestDelegate _next;

    public RequestIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // تولید یک GUID برای هر درخواست
        var requestId = ObjectExtensions.GenerateUniqueNumber();

        // ذخیره در HttpContext
        context.Items[RequestIdKey] = requestId;

        // ادامه پردازش
        await _next(context);
    }

    public static string? GetRequestId(HttpContext context)
    {
        return String.Empty;//context.Items.TryGetValue(RequestIdKey, out var id) ? id?.ToString() : null;
    }

}
