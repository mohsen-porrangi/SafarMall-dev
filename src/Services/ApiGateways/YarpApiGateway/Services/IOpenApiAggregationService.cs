namespace YarpApiGateway.Services;

public interface IOpenApiAggregationService
{
    Task<object?> GetAggregatedOpenApiAsync(HttpContext? context = null);
    Task<object?> GetServiceOpenApiAsync(string serviceName, HttpContext? context = null);
    Task<bool> ServiceExistsAsync(string serviceName);
    Task<IEnumerable<string>> GetAvailableServicesAsync();
    Task<Dictionary<string, object>> GetServicesStatusAsync();
}