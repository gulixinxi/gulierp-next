using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace GuliERP.Crm.Infrastructure.Services;

/// <summary>
/// GEO（AI搜索引擎优化）检测服务：通过 geo CLI 检测网站在 AI 搜索引擎中的可见性。
/// 检测9个维度：google_ai/robots/schema/content/brand_entity/meta/llms/signals/ai_discovery
/// 每次检测需 30-90 秒，依赖 playwright 浏览器。
/// </summary>
public class GeoCheckService
{
    private readonly ILogger<GeoCheckService> _logger;
    private const string GeoCli = "geo";
    private const string PlaywrightBrowsersPath = "/home/user/.cache/ms-playwright";

    public GeoCheckService(ILogger<GeoCheckService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 检测单个网站的 GEO 表现。
    /// </summary>
    public async Task<GeoCheckResult> CheckAsync(string domain, CancellationToken ct = default)
    {
        domain = domain.Replace("http://", "").Replace("https://", "").TrimEnd('/').Split('/')[0];
        var result = new GeoCheckResult { Domain = domain };

        try
        {
            // 优先尝试 HTTPS，失败则尝试 HTTP
            var url = $"https://{domain}";
            var raw = await RunGeoAsync(url, ct);
            if (raw == null || raw.Contains("\"error\": \"Connection failed\"") || raw.Contains("\"score\": 0"))
            {
                // 尝试 HTTP
                url = $"http://{domain}";
                raw = await RunGeoAsync(url, ct);
            }

            if (string.IsNullOrWhiteSpace(raw))
            {
                result.Error = "geo CLI 返回空";
                return result;
            }

            // 提取 JSON
            var jsonStart = raw.IndexOf('{');
            if (jsonStart < 0)
            {
                result.Error = "无法解析 geo 输出";
                return result;
            }

            using var doc = JsonDocument.Parse(raw[jsonStart..]);
            var root = doc.RootElement;

            result.Url = GetStr(root, "url") ?? url;
            result.Score = GetInt(root, "score") ?? 0;
            result.Band = GetStr(root, "band");
            result.Error = GetStr(root, "error");
            result.ScoreMax = GetInt(root, "score_max") ?? 100;
            // 各维度得分
            var dimensions = new[] { "google_ai", "robots", "schema", "content", "brand_entity", "meta", "llms", "signals", "ai_discovery" };
            result.Dimensions = new Dictionary<string, int>();
            foreach (var dim in dimensions)
            {
                if (root.TryGetProperty(dim, out var dimEl))
                {
                    result.Dimensions[dim] = GetInt(dimEl, "points") ?? 0;
                }
            }

            result.Checked = true;
            _logger.LogInformation("GEO检测完成 {Domain}: score={Score} band={Band}", domain, result.Score, result.Band);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GEO检测失败: {Domain}", domain);
            result.Error = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// 批量检测（串行，避免同时启动多个浏览器）。
    /// </summary>
    public async Task<List<GeoCheckResult>> CheckBatchAsync(List<string> domains, CancellationToken ct = default)
    {
        var results = new List<GeoCheckResult>();
        foreach (var domain in domains)
        {
            if (ct.IsCancellationRequested) break;
            var r = await CheckAsync(domain, ct);
            results.Add(r);
            await Task.Delay(1000, ct);
        }
        return results;
    }

    private async Task<string?> RunGeoAsync(string url, CancellationToken ct)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = GeoCli,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            psi.ArgumentList.Add("audit");
            psi.ArgumentList.Add("--url");
            psi.ArgumentList.Add(url);
            psi.ArgumentList.Add("--format");
            psi.ArgumentList.Add("json");
            psi.Environment["PLAYWRIGHT_BROWSERS_PATH"] = PlaywrightBrowsersPath;

            using var proc = Process.Start(psi);
            if (proc == null) return null;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(90));

            var stdout = await proc.StandardOutput.ReadToEndAsync(cts.Token);
            await proc.WaitForExitAsync(cts.Token);
            return stdout;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("geo CLI 执行失败 {Url}: {Msg}", url, ex.Message);
            return null;
        }
    }

    private static string? GetStr(JsonElement el, string prop)
    {
        if (el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String)
            return v.GetString();
        return null;
    }

    private static int? GetInt(JsonElement el, string prop)
    {
        if (el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.Number)
            return v.GetInt32();
        return null;
    }
}

/// <summary>GEO 检测结果</summary>
public class GeoCheckResult
{
    public string Domain { get; set; } = "";
    public string? Url { get; set; }
    public bool Checked { get; set; }
    public int Score { get; set; }
    public int ScoreMax { get; set; } = 100;
    public string? Band { get; set; }       // critical/foundation/good/excellent
    public string? Error { get; set; }
    public Dictionary<string, int> Dimensions { get; set; } = new();
}
