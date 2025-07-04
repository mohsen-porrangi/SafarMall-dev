using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using YarpApiGateway.Middleware;
using YarpApiGateway.Services;

public class OpenApiAggregationService : IOpenApiAggregationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly ILogger<OpenApiAggregationService> _logger;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

    public OpenApiAggregationService(
        HttpClient httpClient,
        IConfiguration configuration,
        IMemoryCache cache,
        ILogger<OpenApiAggregationService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _cache = cache;
        _logger = logger;
    }

    public async Task<object?> GetAggregatedOpenApiAsync(HttpContext? context = null)
    {
        const string cacheKey = "aggregated_openapi";

        if (_cache.TryGetValue(cacheKey, out object? cachedSpec))
        {
            return cachedSpec;
        }

        try
        {
            var services = GetServiceConfigurations();
            var aggregatedSpec = await BuildAggregatedSpecAsync(services, context);

            _cache.Set(cacheKey, aggregatedSpec, _cacheExpiration);
            return aggregatedSpec;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating aggregated OpenAPI specification");
            return null;
        }
    }

    public async Task<object?> GetServiceOpenApiAsync(string serviceName, HttpContext? context = null)
    {
        var cacheKey = $"service_openapi_{serviceName}";

        if (_cache.TryGetValue(cacheKey, out object? cachedSpec))
        {
            return cachedSpec;
        }

        try
        {
            var services = GetServiceConfigurations();
            var service = services.FirstOrDefault(s =>
                s.Key.Equals(serviceName, StringComparison.OrdinalIgnoreCase));

            if (service.Key == null)
            {
                return null;
            }

            var spec = await FetchServiceOpenApiAsync(service.Key, service.Value);
            if (spec != null)
            {
                // Filter out internal APIs but DON'T modify schema references for individual services
                if (service.Value.PublicOnly)
                {
                    spec = FilterPublicAPIsOnly(spec, true);
                }

                // Update server URLs to point to gateway with correct prefix
                spec = UpdateServerUrlsInSpec(spec, context, service.Key);

                _cache.Set(cacheKey, spec, _cacheExpiration);
            }

            return spec;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching OpenAPI for service {ServiceName}", serviceName);
            return null;
        }
    }

    public async Task<bool> ServiceExistsAsync(string serviceName)
    {
        var services = await GetAvailableServicesAsync();
        return services.Contains(serviceName, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IEnumerable<string>> GetAvailableServicesAsync()
    {
        const string cacheKey = "available_services";

        if (_cache.TryGetValue(cacheKey, out IEnumerable<string>? cachedServices))
        {
            return cachedServices ?? Enumerable.Empty<string>();
        }

        try
        {
            var services = GetServiceConfigurations();
            var availableServices = new List<string>();

            foreach (var service in services)
            {
                var isHealthy = await CheckServiceHealthAsync(service.Value);
                if (isHealthy)
                {
                    availableServices.Add(service.Key);
                }
            }

            _cache.Set(cacheKey, availableServices, TimeSpan.FromMinutes(2));
            return availableServices;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available services");
            return Enumerable.Empty<string>();
        }
    }

    public async Task<Dictionary<string, object>> GetServicesStatusAsync()
    {
        var services = GetServiceConfigurations();
        var statusDict = new Dictionary<string, object>();

        var tasks = services.Select(async service =>
        {
            var status = await GetServiceDetailedStatusAsync(service.Key, service.Value);
            return new { Service = service.Key, Status = status };
        });

        var results = await Task.WhenAll(tasks);

        foreach (var result in results)
        {
            statusDict[result.Service] = result.Status;
        }

        return statusDict;
    }

    private Dictionary<string, ServiceConfiguration> GetServiceConfigurations()
    {
        var services = new Dictionary<string, ServiceConfiguration>();

        // Read from YARP configuration
        var yarpConfig = _configuration.GetSection("ReverseProxy");
        var clusters = yarpConfig.GetSection("Clusters").GetChildren();

        foreach (var cluster in clusters)
        {
            var clusterName = cluster.Key;
            var destinations = cluster.GetSection("Destinations").GetChildren();

            foreach (var destination in destinations)
            {
                var address = destination.GetValue<string>("Address");
                if (!string.IsNullOrEmpty(address))
                {
                    services[clusterName] = new ServiceConfiguration
                    {
                        Name = clusterName,
                        BaseUrl = address.TrimEnd('/'),
                        OpenApiPath = "/openapi/v1.json",
                        HealthCheckPath = "/health",
                        PublicOnly = true
                    };
                    break;
                }
            }
        }

        // Read custom OpenAPI configuration and override/supplement
        var openApiConfig = _configuration.GetSection("OpenApiServices").GetChildren();
        foreach (var config in openApiConfig)
        {
            var serviceName = config.Key;

            // If service exists from YARP config, update it
            if (services.ContainsKey(serviceName))
            {
                // Try to get BaseUrl from OpenApiServices config, fallback to YARP config
                var configBaseUrl = config.GetValue<string>("BaseUrl");
                if (!string.IsNullOrEmpty(configBaseUrl))
                {
                    services[serviceName].BaseUrl = configBaseUrl.TrimEnd('/');
                }

                services[serviceName].OpenApiPath = config.GetValue<string>("OpenApiPath") ?? "/openapi/v1.json";
                services[serviceName].HealthCheckPath = config.GetValue<string>("HealthCheckPath") ?? "/health";
                services[serviceName].DisplayName = config.GetValue<string>("DisplayName") ?? serviceName;
                services[serviceName].Description = config.GetValue<string>("Description") ?? "";
                services[serviceName].PublicOnly = config.GetValue<bool>("PublicOnly", true);
            }
            // If service doesn't exist in YARP config, create it from OpenApiServices config
            else
            {
                var baseUrl = config.GetValue<string>("BaseUrl");
                if (!string.IsNullOrEmpty(baseUrl))
                {
                    services[serviceName] = new ServiceConfiguration
                    {
                        Name = serviceName,
                        BaseUrl = baseUrl.TrimEnd('/'),
                        OpenApiPath = config.GetValue<string>("OpenApiPath") ?? "/openapi/v1.json",
                        HealthCheckPath = config.GetValue<string>("HealthCheckPath") ?? "/health",
                        DisplayName = config.GetValue<string>("DisplayName") ?? serviceName,
                        Description = config.GetValue<string>("Description") ?? "",
                        PublicOnly = config.GetValue<bool>("PublicOnly", true)
                    };
                }
            }
        }

        return services;
    }

    private async Task<object?> BuildAggregatedSpecAsync(Dictionary<string, ServiceConfiguration> services, HttpContext? context = null)
    {
        var aggregatedSpec = new
        {
            openapi = "3.0.1",
            info = new
            {
                title = "آفاق سیر API Gateway - API های عمومی",
                description = "مجموعه API های عمومی تمام سرویس‌ها از طریق Gateway",
                version = "1.0.0",
                contact = new
                {
                    name = "آفاق سیر",
                    url = "https://afaqseir.ir"
                }
            },
            servers = new[]
            {
                new { url = GetGatewayBaseUrl(context), description = "API Gateway" }
            },
            paths = new Dictionary<string, object>(),
            components = new
            {
                schemas = new Dictionary<string, object>(),
                securitySchemes = new Dictionary<string, object>
                {
                    ["Bearer"] = new
                    {
                        type = "http",
                        scheme = "bearer",
                        bearerFormat = "JWT"
                    }
                }
            },
            tags = new List<object>()
        };

        var allPaths = new Dictionary<string, object>();
        var allSchemas = new Dictionary<string, object>();
        var allTags = new List<object>();

        foreach (var service in services)
        {
            try
            {
                var serviceSpec = await FetchServiceOpenApiAsync(service.Key, service.Value);
                if (serviceSpec is JsonElement jsonSpec)
                {
                    // Filter out internal APIs if PublicOnly is true
                    if (service.Value.PublicOnly)
                    {
                        serviceSpec = FilterPublicAPIsOnly(serviceSpec, true);
                        jsonSpec = (JsonElement)serviceSpec;
                    }

                    // Add service tag with proper display name
                    allTags.Add(new
                    {
                        name = service.Value.DisplayName,
                        description = service.Value.Description ?? $"APIs مربوط به {service.Value.DisplayName}"
                    });

                    // Merge paths with service prefix
                    if (jsonSpec.TryGetProperty("paths", out var paths))
                    {
                        MergePaths(allPaths, paths, service.Key, service.Value.DisplayName);
                    }

                    // Merge schemas
                    if (jsonSpec.TryGetProperty("components", out var components) &&
                        components.TryGetProperty("schemas", out var schemas))
                    {
                        MergeSchemas(allSchemas, schemas, service.Key);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch OpenAPI spec for service {ServiceName}", service.Key);
            }
        }

        return new
        {
            openapi = aggregatedSpec.openapi,
            info = aggregatedSpec.info,
            servers = aggregatedSpec.servers,
            paths = allPaths,
            components = new
            {
                schemas = allSchemas,
                securitySchemes = aggregatedSpec.components.securitySchemes
            },
            tags = allTags
        };
    }

    private object FilterPublicAPIsOnly(object spec, bool publicOnly)
    {
        if (!publicOnly || spec is not JsonElement jsonSpec)
            return spec;

        if (!jsonSpec.TryGetProperty("paths", out var paths))
            return spec;

        var filteredPaths = new Dictionary<string, object>();

        foreach (var path in paths.EnumerateObject())
        {
            var pathKey = path.Name;

            // Check if this path should be included (not internal)
            if (InternalPathFilterMiddleware.IsPublicPath(pathKey, _configuration))
            {
                var pathValue = JsonSerializer.Deserialize<Dictionary<string, object>>(path.Value.GetRawText());
                if (pathValue != null)
                {
                    filteredPaths[pathKey] = pathValue;
                }
            }
            else
            {
                _logger.LogDebug("Filtering out internal path: {Path}", pathKey);
            }
        }

        // Rebuild the spec with filtered paths
        var originalSpec = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonSpec.GetRawText());
        if (originalSpec != null)
        {
            originalSpec["paths"] = filteredPaths;
            return JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(originalSpec));
        }

        return spec;
    }

    private void MergePaths(Dictionary<string, object> allPaths, JsonElement paths, string serviceName, string displayName)
    {
        foreach (var path in paths.EnumerateObject())
        {
            var pathKey = path.Name;
            var pathValue = path.Value;

            // اضافه کردن prefix مناسب برای هر سرویس
            var gatewayPath = GetGatewayPathPrefix(serviceName, pathKey);

            if (pathValue.ValueKind == JsonValueKind.Object)
            {
                var pathObject = new Dictionary<string, object>();

                foreach (var method in pathValue.EnumerateObject())
                {
                    if (method.Value.ValueKind == JsonValueKind.Object)
                    {
                        var methodObject = JsonSerializer.Deserialize<Dictionary<string, object>>(method.Value.GetRawText());

                        if (methodObject != null)
                        {
                            methodObject["tags"] = new[] { displayName };

                            if (methodObject.TryGetValue("operationId", out var operationId))
                            {
                                methodObject["operationId"] = $"{serviceName}_{operationId}";
                            }

                            // Fix schema references
                            methodObject = UpdateSchemaReferences(methodObject, serviceName);
                        }

                        pathObject[method.Name] = methodObject ?? new Dictionary<string, object>();
                    }
                }

                allPaths[gatewayPath] = pathObject;
            }
        }
    }

    private string GetGatewayPathPrefix(string serviceName, string originalPath)
    {
        // حذف /api از ابتدای originalPath اگر وجود دارد
        var cleanPath = originalPath.StartsWith("/api") ? originalPath[4..] : originalPath;

        // اضافه کردن prefix سرویس
        return $"/api/{serviceName}{cleanPath}";
    }

    private Dictionary<string, object> UpdateSchemaReferences(Dictionary<string, object> methodObject, string serviceName)
    {
        var jsonString = JsonSerializer.Serialize(methodObject);

        // Update all $ref paths to include service prefix
        jsonString = System.Text.RegularExpressions.Regex.Replace(
            jsonString,
            @"""#/components/schemas/([^""]+)""",
            $"\"#/components/schemas/{serviceName}_$1\"");

        return JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString) ?? methodObject;
    }

    private void MergeSchemas(Dictionary<string, object> allSchemas, JsonElement schemas, string serviceName)
    {
        foreach (var schema in schemas.EnumerateObject())
        {
            var schemaKey = $"{serviceName}_{schema.Name}";
            var schemaValue = JsonSerializer.Deserialize<Dictionary<string, object>>(schema.Value.GetRawText());

            if (schemaValue != null)
            {
                // Update any internal $ref within schemas too
                var schemaJson = JsonSerializer.Serialize(schemaValue);
                schemaJson = System.Text.RegularExpressions.Regex.Replace(
                    schemaJson,
                    @"""#/components/schemas/([^""]+)""",
                    $"\"#/components/schemas/{serviceName}_$1\"");

                schemaValue = JsonSerializer.Deserialize<Dictionary<string, object>>(schemaJson) ?? schemaValue;
                allSchemas[schemaKey] = schemaValue;
            }
        }
    }

    private object UpdateServerUrlsInSpec(object spec, HttpContext? context = null, string? serviceName = null)
    {
        if (spec is JsonElement jsonSpec)
        {
            var specDict = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonSpec.GetRawText());
            if (specDict != null)
            {
                // برای individual service، path ها را هم update کن
                if (!string.IsNullOrEmpty(serviceName) && specDict.ContainsKey("paths"))
                {
                    var paths = specDict["paths"] as Dictionary<string, object> ?? new Dictionary<string, object>();
                    var updatedPaths = new Dictionary<string, object>();

                    foreach (var path in paths)
                    {
                        var updatedPath = GetGatewayPathPrefix(serviceName, path.Key);
                        updatedPaths[updatedPath] = path.Value;
                    }

                    specDict["paths"] = updatedPaths;
                }

                specDict["servers"] = new[]
                {
                    new { url = GetGatewayBaseUrl(context), description = "API Gateway" }
                };
                return JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(specDict));
            }
        }
        return spec;
    }

    private async Task<object?> FetchServiceOpenApiAsync(string serviceName, ServiceConfiguration config)
    {
        try
        {
            var url = $"{config.BaseUrl}{config.OpenApiPath}";
            _logger.LogDebug("Fetching OpenAPI spec from {Url} for service {ServiceName}", url, serviceName);

            using var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch OpenAPI spec for {ServiceName}. Status: {StatusCode}",
                    serviceName, response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonSerializer.Deserialize<JsonElement>(content);

            return jsonDoc;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching OpenAPI spec for service {ServiceName}", serviceName);
            return null;
        }
    }

    private async Task<bool> CheckServiceHealthAsync(ServiceConfiguration config)
    {
        try
        {
            var url = $"{config.BaseUrl}{config.HealthCheckPath}";
            using var response = await _httpClient.GetAsync(url);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task<object> GetServiceDetailedStatusAsync(string serviceName, ServiceConfiguration config)
    {
        var isHealthy = await CheckServiceHealthAsync(config);

        return new
        {
            Name = serviceName,
            DisplayName = config.DisplayName,
            BaseUrl = config.BaseUrl,
            Status = isHealthy ? "Healthy" : "Unhealthy",
            OpenApiEndpoint = $"{config.BaseUrl}{config.OpenApiPath}",
            HealthCheckEndpoint = $"{config.BaseUrl}{config.HealthCheckPath}",
            PublicOnly = config.PublicOnly,
            LastChecked = DateTime.UtcNow
        };
    }

    private string GetGatewayBaseUrl(HttpContext? context = null)
    {
        string baseUrl;

        if (context != null)
        {
            try
            {
                var forwardedProto = context.Request.Headers["X-Forwarded-Proto"].FirstOrDefault();
                var forwardedHost = context.Request.Headers["X-Forwarded-Host"].FirstOrDefault();

                if (!string.IsNullOrEmpty(forwardedProto) && !string.IsNullOrEmpty(forwardedHost))
                {
                    // اضافه کردن PathBase به forwarded headers
                    var forwardedPathBase = context.Request.PathBase.Value ?? "";
                    baseUrl = $"{forwardedProto}://{forwardedHost}{forwardedPathBase}";
                    _logger.LogInformation("Using forwarded headers: {BaseUrl}", baseUrl);
                    return baseUrl;
                }

                // استفاده از context.Request.GetDisplayUrl() برای دریافت کامل URL
                var requestUrl = context.Request.GetDisplayUrl();
                var uri = new Uri(requestUrl);
                var requestPathBase = context.Request.PathBase.Value ?? "";

                baseUrl = $"{uri.Scheme}://{uri.Authority}{requestPathBase}";
                _logger.LogInformation("Using request context: {BaseUrl}", baseUrl);
                return baseUrl;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to auto-detect gateway base URL from request context");
            }
        }

        baseUrl = _configuration.GetValue<string>("Gateway:BaseUrl") ?? "http://185.129.170.40:8080";
        _logger.LogInformation("Using configuration fallback: {BaseUrl}", baseUrl);
        return baseUrl;
    }
}

public class ServiceConfiguration
{
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string OpenApiPath { get; set; } = "/openapi/v1.json";
    public string HealthCheckPath { get; set; } = "/health";
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool PublicOnly { get; set; } = true;
}