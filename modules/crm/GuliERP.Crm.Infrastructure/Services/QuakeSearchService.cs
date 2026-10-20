using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GuliERP.Crm.Application;
using GuliERP.Crm.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GuliERP.Crm.Infrastructure.Services;

/// <summary>
/// Quake 360 网络空间测绘搜索服务。
/// 调用 Quake API 搜索目标站点，返回域名、IP、标题、位置等线索候选。
/// </summary>
public class QuakeSearchService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _configuration;
    private readonly ILogger<QuakeSearchService> _logger;
    private const string QuakeApiUrl = "https://quake.360.net/api/v3/search/quake_service";

    public QuakeSearchService(HttpClient http, IConfiguration configuration, ILogger<QuakeSearchService> logger)
    {
        _http = http;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// 搜索 Quake，返回简化后的线索候选列表。
    /// </summary>
    public async Task<List<QuakeSiteResult>> SearchAsync(string query, int size = 20, CancellationToken ct = default)
    {
        var apiKey = _configuration["Quake:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Quake API key is not configured.");

        // 手动构造 JSON，避免 JsonContent 自动加 charset 导致 Quake 报错
        var jsonBody = System.Text.Json.JsonSerializer.Serialize(new
        {
            query,
            start = 0,
            size,
            ignore_cache = false
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, QuakeApiUrl)
        {
            Content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json"),
            Headers = { { "X-QuakeToken", apiKey } }
        };

        try
        {
            var response = await _http.SendAsync(request, ct);
            var raw = await response.Content.ReadAsStringAsync(ct);
            _logger.LogInformation("Quake API HTTP {StatusCode}, response length {Len}", response.StatusCode, raw.Length);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Quake API 失败: {StatusCode}", response.StatusCode);
                throw new Exception($"Quake API 返回 {response.StatusCode}");
            }

            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            var code = GetInt(root, "code");
            if (code.HasValue && code.Value != 0)
            {
                throw new InvalidOperationException($"Quake API business error code {code.Value}.");
            }

            var results = new List<QuakeSiteResult>();
            if (!root.TryGetProperty("data", out var dataArr) || dataArr.ValueKind != JsonValueKind.Array)
                return results;

            foreach (var item in dataArr.EnumerateArray())
            {
                var domain = GetString(item, "domain") ?? GetString(item, "hostname") ?? GetString(item, "ip") ?? "";
                if (string.IsNullOrWhiteSpace(domain)) continue;

                string? title = null;
                if (item.TryGetProperty("service", out var svc) && svc.ValueKind == JsonValueKind.Object
                    && svc.TryGetProperty("http", out var http) && http.ValueKind == JsonValueKind.Object)
                {
                    title = GetString(http, "title");
                }

                string? city = null;
                string? province = null;
                string? isp = null;
                if (item.TryGetProperty("location", out var loc) && loc.ValueKind == JsonValueKind.Object)
                {
                    city = GetString(loc, "city_cn");
                    province = GetString(loc, "province_cn");
                    isp = GetString(loc, "isp");
                }

                results.Add(new QuakeSiteResult
                {
                    Domain = domain,
                    Ip = GetString(item, "ip"),
                    Port = GetInt(item, "port"),
                    Title = title,
                    Service = GetString(item, "service_name"),
                    City = city,
                    Province = province,
                    Isp = isp,
                    Os = GetString(item, "os")
                });
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Quake 搜索异常");
            throw;
        }
    }

    private static string? GetString(JsonElement el, string prop)
    {
        if (el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String)
            return v.GetString();
        return null;
    }

    private static int? GetInt(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var v)) return null;
        if (v.ValueKind == JsonValueKind.Number) return v.GetInt32();
        if (v.ValueKind == JsonValueKind.String && int.TryParse(v.GetString(), out var n)) return n;
        return null;
    }
}

/// <summary>Quake 搜索结果（简化后的线索候选）</summary>
public class QuakeSiteResult
{
    public string Domain { get; set; } = "";
    public string? Ip { get; set; }
    public int? Port { get; set; }
    public string? Title { get; set; }
    public string? Service { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? Isp { get; set; }
    public string? Os { get; set; }
}
