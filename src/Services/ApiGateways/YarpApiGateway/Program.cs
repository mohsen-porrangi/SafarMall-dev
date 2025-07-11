using YarpApiGateway.Middleware;
using YarpApiGateway.Services;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Add OpenAPI services
builder.Services.AddOpenApi("gateway", options =>
{
    options.AddDocumentTransformer<GatewayDocumentTransformer>();
});

builder.Services.AddHttpClient();

// Add memory cache
builder.Services.AddMemoryCache();

// Add custom services
builder.Services.AddSingleton<IOpenApiAggregationService, OpenApiAggregationService>();
builder.Services.AddTransient<GatewayDocumentTransformer>();

// Add health checks
builder.Services.AddHealthChecks();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseCors("AllowAll");

// Add custom middleware
app.UseMiddleware<InternalPathFilterMiddleware>();

// Configure OpenAPI in development
//if (app.Environment.IsDevelopment())
//{
// Map OpenAPI endpoints
app.MapOpenApi("gateway");

// Add aggregated OpenAPI endpoint
app.MapGet("/openapi/aggregated.json", async (IOpenApiAggregationService aggregationService, HttpContext context) =>
{
    try
    {
        var aggregatedSpec = await aggregationService.GetAggregatedOpenApiAsync(context);
        return Results.Json(aggregatedSpec, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error generating aggregated OpenAPI: {ex.Message}");
    }
})
.WithName("GetAggregatedOpenApi")
.WithTags("Documentation");

// Add individual service OpenAPI endpoints
app.MapGet("/openapi/{serviceName}.json", async (
    string serviceName,
    IOpenApiAggregationService aggregationService,
    HttpContext context) =>
{
    try
    {
        var spec = await aggregationService.GetServiceOpenApiAsync(serviceName, context);
        if (spec == null)
        {
            return Results.NotFound($"Service '{serviceName}' not found or OpenAPI spec not available");
        }

        return Results.Json(spec, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error getting OpenAPI for service '{serviceName}': {ex.Message}");
    }
})
    .WithName("GetServiceOpenApi")
    .WithTags("Documentation");

// Main Scalar UI (Aggregated)
app.MapScalarApiReference("scalar", options =>
{
    options.Title = "آفاق سیر API Gateway - تمام سرویس‌ها";
    options.Theme = ScalarTheme.Kepler;
    options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
    options.OpenApiRoutePattern = "/openapi/aggregated.json";
    options.EndpointPathPrefix = "/docs";
    options.ShowSidebar = true;
    options.HideDownloadButton = false;
    options.SearchHotKey = "k";
    options.Servers = new[]
    {
            new ScalarServer($"{GetBaseUrl(builder.Configuration)}", "API Gateway")
    };
});

// Individual service documentation endpoints
app.MapGet("/docs/{serviceName}", async (string serviceName, HttpContext context,
    IOpenApiAggregationService aggregationService) =>
{
    // Check if service exists
    var serviceExists = await aggregationService.ServiceExistsAsync(serviceName);
    if (!serviceExists)
    {
        return Results.NotFound($"سرویس '{serviceName}' یافت نشد");
    }

    var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
    var html = GenerateScalarHtml(serviceName, $"/openapi/{serviceName}.json", baseUrl);

    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.WriteAsync(html);
    return Results.Empty;
})
.WithName("GetServiceDocs")
.WithTags("Documentation");

// Services list endpoint
app.MapGet("/docs", async (IOpenApiAggregationService aggregationService, HttpContext context) =>
{
    var services = await aggregationService.GetAvailableServicesAsync();
    var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";

    var html = GenerateServicesListHtml(services, baseUrl);
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.WriteAsync(html);
    return Results.Empty;
})
.WithName("GetServicesList")
.WithTags("Documentation");

// API status endpoint
app.MapGet("/api/status", async (IOpenApiAggregationService aggregationService) =>
{
    var services = await aggregationService.GetServicesStatusAsync();
    return Results.Json(new
    {
        Gateway = new
        {
            Status = "Healthy",
            Version = "1.0.0",
            Environment = app.Environment.EnvironmentName,
            Timestamp = DateTime.UtcNow
        },
        Services = services
    });
})
.WithName("GetApiStatus")
.WithTags("Monitoring");
//}

app.UseCors("AllowAll");

// Map reverse proxy
app.MapReverseProxy();

// Add health check endpoint
app.MapHealthChecks("/health");

// Add info endpoint
app.MapGet("/info", () => new
{
    Service = "آفاق سیر API Gateway",
    Version = "1.0.0",
    Environment = app.Environment.EnvironmentName,
    Timestamp = DateTime.UtcNow,
    Documentation = new
    {
        AllServices = "/docs",
        AggregatedDocs = "/docs/scalar",
        OpenApiSpec = "/openapi/aggregated.json"
    }
})
.WithName("GetInfo")
.WithTags("Information");

app.Run();

// Helper methods
static string GetBaseUrl(IConfiguration configuration)
{
    // فقط از configuration بخون
    return configuration.GetValue<string>("Gateway:BaseUrl") ?? "https://localhost:7158";
}

static void UpdateServerUrls(object spec, HttpContext context)
{
    if (spec is JsonElement jsonSpec && jsonSpec.TryGetProperty("servers", out var servers))
    {
        var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
        // Update servers to point to gateway instead of individual services
        // This ensures all API calls go through the gateway
    }
}

static string GenerateScalarHtml(string serviceName, string openApiPath, string baseUrl)
{
    return $@"
<!DOCTYPE html>
<html dir=""rtl"" lang=""fa"">
<head>
    <title>مستندات API - {serviceName}</title>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1"" />
    <style>
         @font-face {{
            font-family: 'Inter';
            src: local('Segoe UI'), local('Tahoma'), local('Arial');
        }}
     
        @font-face {{
            font-family: 'JetBrains Mono';
            src: local('Courier New'), local('monospace');
        }}
     
        :root {{
            --font-body: 'Inter', sans-serif;
            --font-mono: 'JetBrains Mono', monospace;
        }}
     
        body {{
            font-family: var(--font-body);
            margin: 0;
            padding: 0;
        }}
     
        .header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 1rem;
            text-align: center;
            margin-bottom: 1rem;
        }}
     
        .nav-link {{
            color: white;
            text-decoration: none;
            margin: 0 10px;
            padding: 5px 10px;
            border-radius: 5px;
            transition: background-color 0.3s;
        }}
     
        .nav-link:hover {{
            background-color: rgba(255,255,255,0.2);
        }}
    </style>
</head>
<body>
    <div class=""header"">
        <h1>مستندات API - {serviceName}</h1>
        <nav>
            <a href=""{baseUrl}/docs"" class=""nav-link"">🏠 فهرست سرویس‌ها</a>
            <a href=""{baseUrl}/docs/scalar"" class=""nav-link"">📚 مستندات کامل</a>
            <a href=""{baseUrl}/health"" class=""nav-link"">💚 وضعیت سلامت</a>
        </nav>
    </div>
    <script
        id=""api-reference""
        data-url=""{openApiPath}""
        data-configuration='{{
            ""theme"": ""kepler"",
            ""showSidebar"": true,
            ""hideDownloadButton"": false,
            ""searchHotKey"": ""k"",
            ""servers"": [
                {{
                    ""url"": ""{baseUrl}"",
                    ""description"": ""API Gateway""
                }}
            ]
        }}'>
    </script>
    <script src=""https://cdn.jsdelivr.net/npm/@scalar/api-reference""></script>
</body>
</html>";
}

static string GenerateServicesListHtml(IEnumerable<string> services, string baseUrl)
{
    var servicesList = string.Join("", services.Select(service =>
        $@"<div class=""service-card"">
            <h3>🔧 {service}</h3>
            <div class=""service-actions"">
                <a href=""{baseUrl}/docs/{service}"" class=""btn btn-primary"">📖 مستندات</a>
                <a href=""{baseUrl}/openapi/{service}.json"" class=""btn btn-secondary"">📄 OpenAPI JSON</a>
            </div>
        </div>"));

    return $@"
<!DOCTYPE html>
<html dir=""rtl"" lang=""fa"">
<head>
    <title>آفاق سیر API Gateway - فهرست سرویس‌ها</title>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1"" />
    <style>
        body {{ 
            font-family: 'Segoe UI', Tahoma, Arial, sans-serif;
            margin: 0;
            padding: 0;
            background: #f5f5f5;
        }}
        .header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 2rem;
            text-align: center;
        }}
        .container {{
            max-width: 1200px;
            margin: 0 auto;
            padding: 2rem;
        }}
        .services-grid {{
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
            gap: 1.5rem;
            margin-top: 2rem;
        }}
        .service-card {{
            background: white;
            padding: 1.5rem;
            border-radius: 10px;
            box-shadow: 0 4px 6px rgba(0,0,0,0.1);
            transition: transform 0.3s, box-shadow 0.3s;
        }}
        .service-card:hover {{
            transform: translateY(-5px);
            box-shadow: 0 8px 15px rgba(0,0,0,0.2);
        }}
        .service-actions {{
            margin-top: 1rem;
        }}
        .btn {{
            display: inline-block;
            padding: 8px 16px;
            margin: 5px;
            text-decoration: none;
            border-radius: 5px;
            transition: background-color 0.3s;
        }}
        .btn-primary {{
            background: #667eea;
            color: white;
        }}
        .btn-secondary {{
            background: #6c757d;
            color: white;
        }}
        .btn:hover {{
            opacity: 0.8;
        }}
        .main-actions {{
            text-align: center;
            margin: 2rem 0;
        }}
        .main-actions .btn {{
            margin: 0 10px;
            padding: 12px 24px;
            font-size: 1.1rem;
        }}
    </style>
</head>
<body>
    <div class=""header"">
        <h1>🚀 آفاق سیر API Gateway</h1>
        <p>مستندات و مدیریت تمام سرویس‌های API</p>
    </div>
    
    <div class=""container"">
        <div class=""main-actions"">
            <a href=""{baseUrl}/docs/scalar"" class=""btn btn-primary"">📚 مستندات کامل (تمام سرویس‌ها)</a>
            <a href=""{baseUrl}/openapi/aggregated.json"" class=""btn btn-secondary"">📄 OpenAPI کامل</a>
            <a href=""{baseUrl}/api/status"" class=""btn btn-secondary"">📊 وضعیت سرویس‌ها</a>
        </div>
        
        <h2>🔧 سرویس‌های موجود</h2>
        <div class=""services-grid"">
            {servicesList}
        </div>
        
        {(services.Any() ? "" : @"<div style=""text-align: center; padding: 2rem; color: #666;"">
            <h3>هیچ سرویسی یافت نشد</h3>
            <p>لطفاً اطمینان حاصل کنید که سرویس‌ها در حال اجرا هستند و OpenAPI را ارائه می‌دهند.</p>
        </div>")}
    </div>
</body>
</html>";
}

// Document transformer for gateway
public class GatewayDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Info.Title = "آفاق سیر API Gateway";
        document.Info.Description = "Gateway برای دسترسی به تمام سرویس‌های API";
        document.Info.Version = "1.0.0";

        // Add gateway specific endpoints
        return Task.CompletedTask;
    }
}