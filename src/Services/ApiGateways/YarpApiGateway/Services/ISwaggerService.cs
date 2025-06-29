namespace YarpApiGateway.Services;

public interface ISwaggerService
{
    Task<string?> GetFilteredSwaggerJsonAsync(string serviceName);
    Task<Dictionary<string, object>> GetServicesStatusAsync();
}