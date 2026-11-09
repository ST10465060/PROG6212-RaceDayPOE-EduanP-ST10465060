using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Web.Services;

/// <summary>
/// A thin wrapper around HttpClient so the controllers never have to deal
/// with status codes, JSON options or URL building themselves.
/// </summary>
public class ApiClient
{
    private readonly HttpClient _http;
    private readonly ILogger<ApiClient> _logger;

    // The API sends camelCase, our DTOs are PascalCase. Telling the
    // serialiser to ignore casing saves putting an attribute on every
    // single property in the contracts project.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiClient(HttpClient http, ILogger<ApiClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    /// <summary>
    /// Fetches a single item. Returns null for a 404 rather than throwing,
    /// because "not found" is a normal thing for a web page to handle.
    /// </summary>
    public async Task<T?> GetAsync<T>(string url)
    {
        try
        {
            var response = await _http.GetAsync(url);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return default;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        }
        catch (HttpRequestException ex)
        {
            // Almost always means the API is not running. Log it properly
            // so the error page can stay friendly.
            _logger.LogError(ex, "Could not reach the API at {Url}", url);
            return default;
        }
    }

    /// <summary>
    /// Fetches a list. Returns an empty list instead of null so the views
    /// can loop over the result without a null check every time.
    /// </summary>
    public async Task<List<T>> GetListAsync<T>(string url)
    {
        var result = await GetAsync<List<T>>(url);
        return result ?? new List<T>();
    }

    /// <summary>
    /// Posts a request body and gives back both the outcome and the error
    /// text, so the controller can push an API validation message straight
    /// into ModelState.
    /// </summary>
    public async Task<ApiResult<TResponse>> PostAsync<TRequest, TResponse>(
        string url, TRequest body)
    {
        try
        {
            var response = await _http.PostAsJsonAsync(url, body);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions);
                return ApiResult<TResponse>.Ok(data);
            }

            var error = await response.Content.ReadAsStringAsync();
            return ApiResult<TResponse>.Fail(CleanError(error, response.StatusCode));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "POST to {Url} failed", url);
            return ApiResult<TResponse>.Fail("The service is unavailable. Please try again shortly.");
        }
    }

    /// <summary>
    /// For updates, where the API replies 204 with no body.
    /// </summary>
    public async Task<ApiResult<bool>> PutAsync<TRequest>(string url, TRequest body)
    {
        try
        {
            var response = await _http.PutAsJsonAsync(url, body);

            if (response.IsSuccessStatusCode)
            {
                return ApiResult<bool>.Ok(true);
            }

            var error = await response.Content.ReadAsStringAsync();
            return ApiResult<bool>.Fail(CleanError(error, response.StatusCode));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "PUT to {Url} failed", url);
            return ApiResult<bool>.Fail("The service is unavailable. Please try again shortly.");
        }
    }

    public async Task<ApiResult<bool>> DeleteAsync(string url)
    {
        try
        {
            var response = await _http.DeleteAsync(url);

            if (response.IsSuccessStatusCode)
            {
                return ApiResult<bool>.Ok(true);
            }

            var error = await response.Content.ReadAsStringAsync();
            return ApiResult<bool>.Fail(CleanError(error, response.StatusCode));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "DELETE to {Url} failed", url);
            return ApiResult<bool>.Fail("The service is unavailable. Please try again shortly.");
        }
    }

    // Our controllers return plain strings for business rule failures, but
    // the framework returns a ProblemDetails JSON blob for model validation
    // errors. Showing raw JSON to a user is ugly, so catch that case.
    private static string CleanError(string raw, HttpStatusCode status)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return $"The request failed ({(int)status}).";
        }

        if (raw.TrimStart().StartsWith("{"))
        {
            return "Please check the details you entered and try again.";
        }

        return raw.Trim('"');
    }
}

/// <summary>
/// A tiny result type so a controller can ask "did it work, and if not
/// what do I tell the user" without inspecting HTTP status codes.
/// </summary>
public class ApiResult<T>
{
    public bool Success { get; private set; }
    public T? Data { get; private set; }
    public string? ErrorMessage { get; private set; }

    public static ApiResult<T> Ok(T? data) =>
        new() { Success = true, Data = data };

    public static ApiResult<T> Fail(string message) =>
        new() { Success = false, ErrorMessage = message };
}