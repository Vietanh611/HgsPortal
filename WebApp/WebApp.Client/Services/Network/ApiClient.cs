using Hgs.Share.Responses.ApiResponses;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace WebApp.Client.Services.Network;

public class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly Data.ITokenStorage _tokenStorage;
    private readonly NavigationManager _navigationManager;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _apiBaseUrl;
    private int _retryCount = 3;
    private TimeSpan _retryDelay = TimeSpan.FromSeconds(1);

    public bool IsLoading { get; private set; }
    public string? LastError { get; private set; }

    public ApiClient(HttpClient httpClient, Data.ITokenStorage tokenStorage, NavigationManager navigationManager, string apiBaseUrl)
    {
        _httpClient = httpClient;
        _tokenStorage = tokenStorage;
        _navigationManager = navigationManager;
        _apiBaseUrl = apiBaseUrl;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    private async Task<bool> HandleResponse(HttpResponseMessage response, bool silent = false)
    {
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            var currentUri = _navigationManager.Uri;
            var loginUri = _navigationManager.ToAbsoluteUri("/login").ToString();
            var rootUri = _navigationManager.ToAbsoluteUri("/").ToString();
            var domesticDisplayUri = _navigationManager.ToAbsoluteUri("/display/DomesticBaggageArrivalDisplay").ToString();
            var internationalDisplayUri = _navigationManager.ToAbsoluteUri("/display/InternationalBaggageArrivalDisplay").ToString();

            // Don't redirect to login if on display pages (public pages)
            if (!string.Equals(currentUri, loginUri, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(currentUri, rootUri, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(currentUri, domesticDisplayUri, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(currentUri, internationalDisplayUri, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("401 Unauthorized - Redirecting to login");
                await _tokenStorage.ClearTokensAsync();
                _navigationManager.NavigateTo("login", forceLoad: true);
            }
            else
            {
                Console.WriteLine("401 Unauthorized - Staying on current page (login or display page)");
            }

            return false;
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            LastError = "You do not have permission to access this resource.";

            // Trong chế độ silent: không redirect sang trang forbidden (dùng cho
            // các request tùy chọn/nền như đếm module, chỉ trả về mặc định).
            if (!silent)
            {
                var currentUri = _navigationManager.Uri;
                var forbiddenUri = _navigationManager.ToAbsoluteUri("/forbidden").ToString();

                // Don't redirect if already on the forbidden page (avoid reload loop)
                if (!string.Equals(currentUri, forbiddenUri, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("403 Forbidden - Redirecting to forbidden page");
                    _navigationManager.NavigateTo("forbidden", forceLoad: true);
                }
            }

            return false;
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();

            try
            {
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(
                    errorContent,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                LastError = apiResponse?.Message
                            ?? $"API Error: {response.StatusCode}";
            }
            catch
            {
                LastError = errorContent;
            }

            Console.WriteLine(LastError);
            return false;
        }

        return true;
    }

    private async Task<T?> ExecuteWithRetry<T>(Func<Task<T>> action, string operationName)
    {
        int attempt = 0;
        Exception? lastException = null;

        while (attempt < _retryCount)
        {
            try
            {
                return await action();
            }
            catch (HttpRequestException ex) when (attempt < _retryCount - 1)
            {
                lastException = ex;
                attempt++;
                Console.WriteLine($"{operationName} failed (attempt {attempt}/{_retryCount}): {ex.Message}. Retrying in {_retryDelay.TotalSeconds}s...");
                await Task.Delay(_retryDelay);
            }
            catch (Exception ex)
            {
                lastException = ex;
                break;
            }
        }

        LastError = $"{operationName} failed after {attempt + 1} attempts: {lastException?.Message}";
        Console.WriteLine(LastError);
        return default;
    }

    private Uri BuildRequestUri(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new ArgumentException("Endpoint cannot be empty.", nameof(endpoint));
        }

        if (Uri.TryCreate(endpoint, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri;
        }

        var baseUri = _httpClient.BaseAddress ?? new Uri(_navigationManager.BaseUri);
        return new Uri(baseUri, endpoint.TrimStart('/'));
    }

    public async Task<T?> GetAsync<T>(string endpoint, bool silent = false, IDictionary<string, string>? headers = null)
    {
        IsLoading = true;
        LastError = null;

        try
        {
            var requestUri = BuildRequestUri(endpoint);
            var result = await ExecuteWithRetry(async () =>
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }

                var response = await _httpClient.SendAsync(request);
                if (await HandleResponse(response, silent))
                {
                    return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
                }
                return default;
            }, $"GET {endpoint}");

            return result;
        }
        catch (Exception ex)
        {
            LastError = $"GET {endpoint} failed: {ex.Message}";
            Console.WriteLine(LastError);
            return default;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<T?> PostAsync<T>(string endpoint, object? data = null)
    {
        IsLoading = true;
        LastError = null;

        try
        {
            var requestUri = BuildRequestUri(endpoint);
            var result = await ExecuteWithRetry(async () =>
            {
                var response = data != null
                    ? await _httpClient.PostAsJsonAsync(requestUri, data, _jsonOptions)
                    : await _httpClient.PostAsync(requestUri, null);

                if (await HandleResponse(response))
                {
                    return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
                }
                return default;
            }, $"POST {endpoint}");

            return result;
        }
        catch (Exception ex)
        {
            LastError = $"POST {endpoint} failed: {ex.Message}";
            Console.WriteLine(LastError);
            return default;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<T?> PutAsync<T>(string endpoint, object data)
    {
        IsLoading = true;
        LastError = null;

        try
        {
            var requestUri = BuildRequestUri(endpoint);
            var result = await ExecuteWithRetry(async () =>
            {
                var response = await _httpClient.PutAsJsonAsync(requestUri, data, _jsonOptions);
                if (await HandleResponse(response))
                {
                    return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
                }
                return default;
            }, $"PUT {endpoint}");

            return result;
        }
        catch (Exception ex)
        {
            LastError = $"PUT {endpoint} failed: {ex.Message}";
            Console.WriteLine(LastError);
            return default;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<T?> DeleteAsync<T>(string endpoint)
    {
        IsLoading = true;
        LastError = null;

        try
        {
            var requestUri = BuildRequestUri(endpoint);
            var result = await ExecuteWithRetry(async () =>
            {
                var response = await _httpClient.DeleteAsync(requestUri);
                if (await HandleResponse(response))
                {
                    return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
                }
                return default;
            }, $"DELETE {endpoint}");

            return result;
        }
        catch (Exception ex)
        {
            LastError = $"DELETE {endpoint} failed: {ex.Message}";
            Console.WriteLine(LastError);
            return default;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<T?> PatchAsync<T>(string endpoint, object data)
    {
        IsLoading = true;
        LastError = null;

        try
        {
            var requestUri = BuildRequestUri(endpoint);
            var result = await ExecuteWithRetry(async () =>
            {
                using var request = new HttpRequestMessage(HttpMethod.Patch, requestUri)
                {
                    Content = JsonContent.Create(data, null, _jsonOptions)
                };
                var response = await _httpClient.SendAsync(request);
                if (await HandleResponse(response))
                {
                    return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
                }
                return default;
            }, $"PATCH {endpoint}");

            return result;
        }
        catch (Exception ex)
        {
            LastError = $"PATCH {endpoint} failed: {ex.Message}";
            Console.WriteLine(LastError);
            return default;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<T?> PostFileAsync<T>(string endpoint, Stream fileStream, string fileName, string contentType)
    {
        IsLoading = true;
        LastError = null;

        try
        {
            var requestUri = BuildRequestUri(endpoint);
            var result = await ExecuteWithRetry(async () =>
            {
                using var form = new MultipartFormDataContent();
                using var fileContent = new StreamContent(fileStream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                form.Add(fileContent, "file", fileName);

                var response = await _httpClient.PostAsync(requestUri, form);
                if (await HandleResponse(response))
                {
                    return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
                }
                return default;
            }, $"POST {endpoint}");

            return result;
        }
        catch (Exception ex)
        {
            LastError = $"POST {endpoint} failed: {ex.Message}";
            Console.WriteLine(LastError);
            return default;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<byte[]?> GetFileBytesAsync(string endpoint)
    {
        IsLoading = true;
        LastError = null;

        try
        {
            var requestUri = BuildRequestUri(endpoint);
            var result = await ExecuteWithRetry(async () =>
            {
                var response = await _httpClient.GetAsync(requestUri);
                if (await HandleResponse(response))
                {
                    return await response.Content.ReadAsByteArrayAsync();
                }
                return null;
            }, $"GET {endpoint}");

            return result;
        }
        catch (Exception ex)
        {
            LastError = $"GET {endpoint} failed: {ex.Message}";
            Console.WriteLine(LastError);
            return null;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public string ResolveUrl(string? relativeOrAbsoluteUrl)
    {
        if (string.IsNullOrWhiteSpace(relativeOrAbsoluteUrl))
        {
            return string.Empty;
        }

        if (Uri.TryCreate(relativeOrAbsoluteUrl, UriKind.Absolute, out _))
        {
            return relativeOrAbsoluteUrl;
        }

        var baseUri = !string.IsNullOrWhiteSpace(_apiBaseUrl)
            ? new Uri(_apiBaseUrl)
            : (_httpClient.BaseAddress ?? new Uri(_navigationManager.BaseUri));
        return new Uri(baseUri, relativeOrAbsoluteUrl.TrimStart('/')).ToString();
    }

    public async Task<bool> PostAsync(string endpoint, object? data = null)
    {
        IsLoading = true;
        LastError = null;

        try
        {
            var result = await ExecuteWithRetry(async () =>
            {
                var response = data != null
                    ? await _httpClient.PostAsJsonAsync(endpoint, data, _jsonOptions)
                    : await _httpClient.PostAsync(endpoint, null);

                return await HandleResponse(response);
            }, $"POST {endpoint}");

            return result;
        }
        catch (Exception ex)
        {
            LastError = $"POST {endpoint} failed: {ex.Message}";
            Console.WriteLine(LastError);
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<bool> PutAsync(string endpoint, object data)
    {
        IsLoading = true;
        LastError = null;

        try
        {
            var result = await ExecuteWithRetry(async () =>
            {
                var response = await _httpClient.PutAsJsonAsync(endpoint, data, _jsonOptions);
                return await HandleResponse(response);
            }, $"PUT {endpoint}");

            return result;
        }
        catch (Exception ex)
        {
            LastError = $"PUT {endpoint} failed: {ex.Message}";
            Console.WriteLine(LastError);
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<bool> DeleteAsync(string endpoint)
    {
        IsLoading = true;
        LastError = null;

        try
        {
            var result = await ExecuteWithRetry(async () =>
            {
                var response = await _httpClient.DeleteAsync(endpoint);
                return await HandleResponse(response);
            }, $"DELETE {endpoint}");

            return result;
        }
        catch (Exception ex)
        {
            LastError = $"DELETE {endpoint} failed: {ex.Message}";
            Console.WriteLine(LastError);
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<bool> PatchAsync(string endpoint, object data)
    {
        IsLoading = true;
        LastError = null;

        try
        {
            var result = await ExecuteWithRetry(async () =>
            {
                using var request = new HttpRequestMessage(HttpMethod.Patch, endpoint)
                {
                    Content = JsonContent.Create(data, null, _jsonOptions)
                };
                var response = await _httpClient.SendAsync(request);
                return await HandleResponse(response);
            }, $"PATCH {endpoint}");

            return result;
        }
        catch (Exception ex)
        {
            LastError = $"PATCH {endpoint} failed: {ex.Message}";
            Console.WriteLine(LastError);
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void SetRetryPolicy(int retryCount, TimeSpan retryDelay)
    {
        _retryCount = retryCount;
        _retryDelay = retryDelay;
    }

    public void ClearError()
    {
        LastError = null;
    }
}
