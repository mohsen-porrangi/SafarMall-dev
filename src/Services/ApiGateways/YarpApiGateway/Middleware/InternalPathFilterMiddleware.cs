namespace YarpApiGateway.Middleware;

public class InternalPathFilterMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<InternalPathFilterMiddleware> _logger;
    private readonly HashSet<string> _internalPaths;
    private readonly IConfiguration _configuration;

    public InternalPathFilterMiddleware(
        RequestDelegate next,
        ILogger<InternalPathFilterMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _configuration = configuration;

        // Load internal patterns from configuration
        var patterns = _configuration.GetSection("InternalApiPatterns")
            .Get<string[]>() ?? Array.Empty<string>();

        _internalPaths = new HashSet<string>(patterns, StringComparer.OrdinalIgnoreCase);

        // Add default patterns if none configured
        if (_internalPaths.Count == 0)
        {
            _internalPaths.Add("/api/internal/");
            _internalPaths.Add("/internal/");
            _internalPaths.Add("/admin/internal/");
            _internalPaths.Add("/api/admin/");
            _internalPaths.Add("/health");
            _internalPaths.Add("/metrics");
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value;

        if (IsInternalPath(path))
        {
            _logger.LogWarning("Blocked internal API access attempt: {Path} from {RemoteIP}",
                path, context.Connection.RemoteIpAddress);

            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json; charset=utf-8";

            var errorResponse = new
            {
                error = "Forbidden",
                message = "دسترسی به API های داخلی از طریق Gateway امکان‌پذیر نیست",
                statusCode = 403,
                timestamp = DateTime.UtcNow,
                path = context.Request.Path.Value
            };

            await context.Response.WriteAsync(
                System.Text.Json.JsonSerializer.Serialize(errorResponse,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                }));
            return;
        }

        // Log request in development
        var environment = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
        if (environment.IsDevelopment())
        {
            _logger.LogDebug("Gateway Request: {Method} {Path} -> Forwarding to backend",
                context.Request.Method, path);
        }

        await _next(context);
    }

    private bool IsInternalPath(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        // Check exact matches first
        if (_internalPaths.Contains(path))
            return true;

        // Check if path starts with any internal pattern
        return _internalPaths.Any(internalPath =>
            path.StartsWith(internalPath, StringComparison.OrdinalIgnoreCase) ||
            path.Contains(internalPath, StringComparison.OrdinalIgnoreCase));
    }

    // Helper method to check if a path should be included in OpenAPI docs
    public static bool IsPublicPath(string? path, IConfiguration configuration)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        var patterns = configuration.GetSection("InternalApiPatterns")
            .Get<string[]>() ?? Array.Empty<string>();

        // If no patterns configured, allow all
        if (patterns.Length == 0)
            return true;

        // Check if path contains any internal pattern
        return !patterns.Any(pattern =>
            path.StartsWith(pattern, StringComparison.OrdinalIgnoreCase) ||
            path.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }
}