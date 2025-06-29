using Simple.Application.model.Enums;

namespace BuildingBlocks.Contracts;
public interface IIntegrationService
{
    Task<T?> PostAsync<T>(string baseURL, string? actionName, object? payload, ContentTypeEnums contentType, string? tokenType = null, string? token = null, CancellationToken cancellationToken = default);
    Task<T?> GetAsync<T>(string baseURL, string? actionName, object? queryModel, string? paramKey = null, string? tokenType = null, string? token = null, CancellationToken cancellationToken = default);
}
