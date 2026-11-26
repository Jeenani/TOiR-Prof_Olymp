using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ToirProf.Desktop.Services;

public class ApiResult<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public int? StatusCode { get; set; }
    public bool IsNetworkError { get; set; }
    public string ErrorMessage { get; set; } = "";

    public static ApiResult<T> Ok(T data, int statusCode = 200) =>
        new() { IsSuccess = true, Data = data, StatusCode = statusCode };

    public static ApiResult<T> Fail(string message, int? statusCode = null, bool isNetwork = false) =>
        new() { IsSuccess = false, ErrorMessage = message, StatusCode = statusCode, IsNetworkError = isNetwork };
}

public partial class ApiClient
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string BaseUrl { get; private set; }
    public string? AuthToken { get; private set; }

    public ApiClient(string baseUrl = "http://localhost:5000")
    {
        BaseUrl = baseUrl.TrimEnd('/');
        _http = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl + "/"),
            Timeout = TimeSpan.FromSeconds(12)
        };
    }

    public void SetBaseUrl(string baseUrl)
    {
        BaseUrl = baseUrl.TrimEnd('/');
        _http.BaseAddress = new Uri(BaseUrl + "/");
    }

    public void SetToken(string? token)
    {
        AuthToken = token;
        _http.DefaultRequestHeaders.Authorization = string.IsNullOrWhiteSpace(token)
            ? null
            : new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string relativeUrl, object? body = null)
    {
        try
        {
            using var req = new HttpRequestMessage(method, relativeUrl.TrimStart('/'));
            if (body != null)
            {
                var json = JsonSerializer.Serialize(body, JsonOptions);
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            using var resp = await _http.SendAsync(req);
            var raw = await resp.Content.ReadAsStringAsync();

            if (resp.IsSuccessStatusCode)
            {
                if (typeof(T) == typeof(string))
                    return ApiResult<T>.Ok((T)(object)raw, (int)resp.StatusCode);

                if (string.IsNullOrWhiteSpace(raw))
                    return ApiResult<T>.Ok(default!, (int)resp.StatusCode);

                var data = JsonSerializer.Deserialize<T>(raw, JsonOptions);
                return ApiResult<T>.Ok(data!, (int)resp.StatusCode);
            }

            var serverMsg = ExtractMessage(raw, resp.StatusCode);
            ErrorLogService.Log($"{method} {relativeUrl}", "HttpError", (int)resp.StatusCode, serverMsg, raw);
            return ApiResult<T>.Fail(serverMsg, (int)resp.StatusCode, false);
        }
        catch (HttpRequestException ex)
        {
            var msg = $"Сервер API недоступен ({BaseUrl}). Проверьте подключение к сети или запуск сервера.";
            ErrorLogService.Log($"{method} {relativeUrl}", "NetworkError", null, msg, ex.Message);
            return ApiResult<T>.Fail(msg, null, true);
        }
        catch (TaskCanceledException ex)
        {
            var msg = "Превышено время ожидания ответа от сервера (тайм-аут).";
            ErrorLogService.Log($"{method} {relativeUrl}", "TimeoutError", null, msg, ex.Message);
            return ApiResult<T>.Fail(msg, null, true);
        }
        catch (Exception ex)
        {
            var msg = $"Непредвиденная ошибка клиента: {ex.Message}";
            ErrorLogService.Log($"{method} {relativeUrl}", "ClientError", null, msg, ex.ToString());
            return ApiResult<T>.Fail(msg, null, false);
        }
    }

    private static string ExtractMessage(string raw, HttpStatusCode status)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(raw))
            {
                using var doc = JsonDocument.Parse(raw);
                if (doc.RootElement.TryGetProperty("message", out var msgProp))
                    return msgProp.GetString() ?? raw;
                if (doc.RootElement.TryGetProperty("title", out var titleProp))
                    return titleProp.GetString() ?? raw;
            }
        }
        catch { }

        return status switch
        {
            HttpStatusCode.BadRequest => "Ошибка валидации входных данных (400).",
            HttpStatusCode.Unauthorized => "Неверный логин или пароль, либо истекла сессия (401).",
            HttpStatusCode.Forbidden => "Недостаточно прав доступа для этой операции (403).",
            HttpStatusCode.NotFound => "Запрашиваемый объект не найден (404).",
            HttpStatusCode.Conflict => "Конфликт данных или занятости ресурсов (409).",
            HttpStatusCode.TooManyRequests => "Превышен лимит запросов (429). Повторите позже.",
            HttpStatusCode.InternalServerError => "Внутренняя ошибка сервера API (500).",
            _ => $"Ошибка HTTP {(int)status}"
        };
    }
}
