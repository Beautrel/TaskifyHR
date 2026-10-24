using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace TaskifyHR.Services;

/// <summary>Réglages de connexion à l'API. C'est ICI qu'on change le mode et l'adresse.</summary>
public static class ApiConfig
{
    /// <summary>true = vraie API Django ; false = données simulées (mode secours).</summary>
    public static readonly bool UseApi = false;

    /// <summary>Adresse IP du PC (commande ipconfig, "Adresse IPv4") — utilisée par un VRAI téléphone.</summary>
    public const string PcIp = "127.0.0.1";

    public static string BaseUrl
    {
        get
        {
#if WINDOWS
            return "http://localhost:8000/api/";
#else
            // Émulateur Android : 10.0.2.2 = le PC. Vrai téléphone : l'IP du PC sur le Wi-Fi.
            return DeviceInfo.Current.DeviceType == DeviceType.Virtual
                ? "http://10.0.2.2:8000/api/"
                : $"http://{PcIp}:8000/api/";
#endif
        }
    }
}

/// <summary>Erreur renvoyée par l'API (400, 403, 409...). Le message est déjà en français.</summary>
public class ApiException : InvalidOperationException
{
    public HttpStatusCode Status { get; }
    public string? Code { get; }
    public ApiException(HttpStatusCode status, string message, string? code = null) : base(message)
    {
        Status = status;
        Code = code;
    }
}

/// <summary>Client HTTP unique : garde les jetons JWT et les renouvelle quand ils expirent.</summary>
public class ApiClient
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };

    public string? Access { get; private set; }
    public string? Refresh { get; private set; }

    public void SetTokens(string access, string refresh) { Access = access; Refresh = refresh; }
    public void Clear() { Access = null; Refresh = null; }

    public async Task<JsonElement> RequestAsync(HttpMethod method, string path, object? body = null, bool auth = true)
    {
        var response = await SendOnceAsync(method, path, body, auth);
        if (response.StatusCode == HttpStatusCode.Unauthorized && auth && Refresh is not null && await TryRefreshAsync())
            response = await SendOnceAsync(method, path, body, auth);

        var text = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            var (message, code) = ExtractError(text, response.StatusCode);
            throw new ApiException(response.StatusCode, message, code);
        }
        return text.Length == 0 ? default : JsonDocument.Parse(text).RootElement.Clone();
    }

    private async Task<HttpResponseMessage> SendOnceAsync(HttpMethod method, string path, object? body, bool auth)
    {
        var request = new HttpRequestMessage(method, ApiConfig.BaseUrl + path);
        if (auth && Access is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Access);
        if (body is not null)
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        return await _http.SendAsync(request);
    }

    private async Task<bool> TryRefreshAsync()
    {
        try
        {
            var response = await SendOnceAsync(HttpMethod.Post, "auth/refresh/", new { refresh = Refresh }, false);
            if (!response.IsSuccessStatusCode) { Clear(); return false; }
            var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
            Access = root.GetProperty("access").GetString();
            if (root.TryGetProperty("refresh", out var r) && r.GetString() is { } nr) Refresh = nr;
            return true;
        }
        catch { return false; }
    }

    // Les erreurs DRF : {"detail": "..."} ou {"champ": ["message"]} ou {"code": "...", "detail": "..."}
    private static (string, string?) ExtractError(string text, HttpStatusCode status)
    {
        try
        {
            var root = JsonDocument.Parse(text).RootElement;
            if (root.ValueKind == JsonValueKind.Object)
            {
                string? code = root.TryGetProperty("code", out var c) && c.ValueKind == JsonValueKind.String ? c.GetString() : null;
                if (root.TryGetProperty("detail", out var d) && d.ValueKind == JsonValueKind.String)
                    return (d.GetString()!, code);
                foreach (var prop in root.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.Array && prop.Value.GetArrayLength() > 0)
                        return (prop.Value[0].ToString(), code);
                    if (prop.Value.ValueKind == JsonValueKind.String)
                        return (prop.Value.GetString()!, code);
                }
            }
        }
        catch { /* corps non JSON */ }
        return (status == HttpStatusCode.Unauthorized ? "Session expirée, reconnectez-vous." : $"Erreur du serveur ({(int)status}).", null);
    }
}
