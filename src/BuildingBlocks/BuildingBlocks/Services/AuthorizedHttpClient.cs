using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Services;

public abstract class AuthorizedHttpClient : BaseHttpClient
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    protected AuthorizedHttpClient(
        HttpClient httpClient,
        ILogger logger,
        IHttpContextAccessor httpContextAccessor)
        : base(httpClient, logger)
    {
        _httpContextAccessor = httpContextAccessor;
        EnsureAuthorizationHeader();
    }

    private void EnsureAuthorizationHeader()
    {
        var context = _httpContextAccessor.HttpContext;
        Logger.LogWarning("EnsureAuthorizationHeader - Context is null: {IsNull}", context == null);

        var authHeader = _httpContextAccessor.HttpContext?.Request.Headers.Authorization
            .FirstOrDefault();
        Logger.LogWarning("EnsureAuthorizationHeader - Auth header: {AuthHeader}", authHeader);

        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            var token = authHeader.Replace("Bearer ", "");
            HttpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
    }
}