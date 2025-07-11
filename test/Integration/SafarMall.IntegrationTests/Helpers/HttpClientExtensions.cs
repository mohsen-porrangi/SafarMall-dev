using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SafarMall.IntegrationTests.Helpers;

public static class HttpClientExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static HttpClient CreateTestHttpClient()
    {
        var handler = new HttpClientHandler()
        {
            // Trust all certificates for testing (development certificates)
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        var client = new HttpClient(handler);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Add("User-Agent", "SafarMall-IntegrationTests/1.0");

        return client;
    }

    public static void AddAuthorizationHeader(this HttpClient client, string token)
    {
        if (!string.IsNullOrEmpty(token))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    public static void RemoveAuthorizationHeader(this HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization = null;
    }

    public static async Task<HttpResponseMessage> PostAsJsonAsync<T>(this HttpClient client, string requestUri, T value)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await client.PostAsync(requestUri, content);
    }

    public static async Task<HttpResponseMessage> PutAsJsonAsync<T>(this HttpClient client, string requestUri, T value)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await client.PutAsync(requestUri, content);
    }

    public static async Task<T?> ReadAsJsonAsync<T>(this HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrEmpty(content))
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(content, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to deserialize response content to {typeof(T).Name}. Content: {content}", ex);
        }
    }

    public static async Task<string> ReadAsStringAsync(this HttpResponseMessage response)
    {
        return await response.Content.ReadAsStringAsync();
    }

    public static async Task<T> EnsureSuccessAndReadAsJsonAsync<T>(this HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Request failed with status {response.StatusCode}. Content: {errorContent}");
        }

        var result = await response.ReadAsJsonAsync<T>();
        if (result == null)
        {
            throw new InvalidOperationException($"Response content could not be deserialized to {typeof(T).Name}");
        }

        return result;
    }

    public static async Task EnsureSuccessStatusCodeAsync(this HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Request failed with status {response.StatusCode}. Content: {errorContent}");
        }
    }

    public static void AddCorrelationId(this HttpClient client, string correlationId)
    {
        client.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);
    }

    public static void AddRequestId(this HttpClient client, string requestId)
    {
        client.DefaultRequestHeaders.Add("X-Request-ID", requestId);
    }

    public static async Task<HttpResponseMessage> PostWithRetryAsync(this HttpClient client, string requestUri, HttpContent content, int maxRetries = 3)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                var response = await client.PostAsync(requestUri, content);
                if (response.IsSuccessStatusCode || i == maxRetries - 1)
                {
                    return response;
                }

                // Wait before retry
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, i)));
            }
            catch (Exception ex) when (i < maxRetries - 1)
            {
                // Log exception and retry
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, i)));
            }
        }

        throw new HttpRequestException($"Failed to complete request after {maxRetries} attempts");
    }

    public static async Task<HttpResponseMessage> GetWithRetryAsync(this HttpClient client, string requestUri, int maxRetries = 3)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                var response = await client.GetAsync(requestUri);
                if (response.IsSuccessStatusCode || i == maxRetries - 1)
                {
                    return response;
                }

                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, i)));
            }
            catch (Exception ex) when (i < maxRetries - 1)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, i)));
            }
        }

        throw new HttpRequestException($"Failed to complete request after {maxRetries} attempts");
    }

    public static async Task<HttpResponseMessage> PutWithRetryAsync(this HttpClient client, string requestUri, HttpContent content, int maxRetries = 3)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                var response = await client.PutAsync(requestUri, content);
                if (response.IsSuccessStatusCode || i == maxRetries - 1)
                {
                    return response;
                }

                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, i)));
            }
            catch (Exception ex) when (i < maxRetries - 1)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, i)));
            }
        }

        throw new HttpRequestException($"Failed to complete request after {maxRetries} attempts");
    }

    public static async Task<HttpResponseMessage> DeleteWithRetryAsync(this HttpClient client, string requestUri, int maxRetries = 3)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                var response = await client.DeleteAsync(requestUri);
                if (response.IsSuccessStatusCode || i == maxRetries - 1)
                {
                    return response;
                }

                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, i)));
            }
            catch (Exception ex) when (i < maxRetries - 1)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, i)));
            }
        }

        throw new HttpRequestException($"Failed to complete request after {maxRetries} attempts");
    }
}