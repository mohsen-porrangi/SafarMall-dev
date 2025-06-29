using BuildingBlocks.Contracts;
using BuildingBlocks.Exceptions;
using BuildingBlocks.Extensions;
using BuildingBlocks.Utils.SafeLog;
using Simple.Application.model.Enums;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace UserManagement.API.Services;
public class IntegrationService : IIntegrationService
{
    // Constants
    private const string HttpClientName = "IntegrationClient";

    // Services
    private readonly IHttpClientFactory _httpClientFactory;

    // Constructor
    public IntegrationService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    // Implementation
    public async Task<T?> PostAsync<T>
        (string baseURL,
        string? actionName,
        object? payload,
        ContentTypeEnums contentType,
        string? tokenType = null,
        string? token = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"{baseURL.TrimEnd('/')}/{actionName?.TrimStart('/')}";


        var httpClient = CreateConfiguredClient(tokenType, token);

        HttpContent content = contentType switch
        {
            ContentTypeEnums.Json => new StringContent(payload?.ToJson(true) ?? "", Encoding.UTF8, "application/json"),
            ContentTypeEnums.FormUrlencodedString => new StringContent(payload?.ToString() ?? "", Encoding.UTF8, "application/x-www-form-urlencoded"),
            ContentTypeEnums.FormUrlencodedFormat => new FormUrlEncodedContent(payload as List<KeyValuePair<string?, string?>>
                                                       ?? new List<KeyValuePair<string?, string?>>()),
            _ => throw new NotSupportedException("Unsupported content type")
        };

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };

        var stopwatch = Stopwatch.StartNew();
        var result = await httpClient.SendAsync(request, cancellationToken);
        stopwatch.Stop();

        string tokween = string.Concat(tokenType, token);

        var headers = new Dictionary<string, string>() {
               { "tokenType", $"{tokenType}" },
               { "token", $"{token}" } };

        SafeLog.Request(
           url: "/api/some-endpoint",
           headers: headers,
           body: payload,
           responseStatus: 200,
           responseTimeMs: 120
       );


        return await HandleResponse<T>(result, cancellationToken);
    }


    public async Task<T?> GetAsync<T>(
    string baseURL,
    string? actionName,
    object? queryModel, // مدل ورودی به جای Dictionary
    string? paramKey = null,
    string? tokenType = null,
    string? token = null,
    CancellationToken cancellationToken = default)
    {
        var httpClient = CreateConfiguredClient(tokenType, token);

        string queryString = "";

        if (paramKey is null)
            queryString = queryModel?.ToQueryString() ?? string.Empty;
        else if (paramKey is not null && queryModel is not null)
        {
            var res = queryModel!.ToString();
            queryString = res.ToQueryStringFromString(paramKey) ?? string.Empty;
        }

        var url = $"{baseURL.TrimEnd('/')}/{actionName?.TrimStart('/')}{queryString}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);

        var stopwatch = Stopwatch.StartNew();
        var result = await httpClient.SendAsync(request, cancellationToken);
        stopwatch.Stop();

        var headers = new Dictionary<string, string>() {
               { "tokenType", $"{tokenType}" },
               { "token", $"{token}" } };

        SafeLog.Request(
           url: "/api/some-endpoint",
           headers: headers,
           body: queryModel,
           responseStatus: 200,
           responseTimeMs: 120
       );

        return await HandleResponse<T>(result, cancellationToken);
    }


    // Helper Methods


    private HttpClient CreateConfiguredClient(string? tokenType, string? token)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);

        if (!string.IsNullOrWhiteSpace(token) && !string.IsNullOrWhiteSpace(tokenType))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(tokenType, token);

        return client;
    }


    private async Task<T?> HandleResponse<T>(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        var stringContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => throw new UnauthorizedDomainException(stringContent),
                HttpStatusCode.NotFound => throw new NotFoundException(stringContent),
                _ => throw new InternalServerException(stringContent)
            };
        }

        return stringContent.JsonToType<T>();
    }



    #region commentCode
    //public async Task<T?> PostAsync<T>
    //   (string baseURL,
    //   string? actionName,
    //   object? payload,
    //   ContentTypeEnums contentType,
    //   string tokenType,
    //   string token,
    //   CancellationToken cancellationToken)
    //{
    //    var httpClient = CreateConfiguredClient(tokenType, token);
    //    HttpContent content;

    //    if (contentType == ContentTypeEnums.Json)
    //        content = new StringContent(payload?.ToJson() ?? "", Encoding.UTF8, "application/json");
    //    else if (contentType == ContentTypeEnums.FormUrlencodedString)
    //        content = new StringContent(payload.ToString() ?? "", Encoding.UTF8, "application/x-www-form-urlencoded");
    //    else
    //        content = new FormUrlEncodedContent(payload as List<KeyValuePair<string?, string?>>);

    //    var result = await httpClient.PostAsync($"{baseURL}{actionName}", content, cancellationToken);
    //    return await HandleResponse<T>(result, cancellationToken);
    //}
    #endregion
}