using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace GuliERP.Crm.Infrastructure.Services;

/// <summary>
/// 网站基础检测服务：可访问性、HTTPS、响应时间、移动端适配、SEO基础元素。
/// 不依赖浏览器，使用 HttpClient 检测。
/// </summary>
public class WebsiteCheckService
{
    private readonly HttpClient _http;
    private readonly ILogger<WebsiteCheckService> _logger;

    public WebsiteCheckService(HttpClient http, ILogger<WebsiteCheckService> logger)
    {
        _http = http;
        _http.Timeout = TimeSpan.FromSeconds(12);
        _logger = logger;
    }

    /// <summary>
    /// 检测单个网站。
    /// </summary>
    public async Task<WebsiteCheckResult> CheckAsync(string domain, CancellationToken ct = default)
    {
        var result = new WebsiteCheckResult { Domain = domain };

        // 去掉域名中的协议前缀和路径
        domain = domain.Replace("http://", "").Replace("https://", "").TrimEnd('/').Split('/')[0];
        result.Domain = domain;

        // 1. 检测 HTTPS
        try
        {
            var httpsSw = Stopwatch.StartNew();
            var httpsResp = await _http.GetAsync($"https://{domain}", ct);
            httpsSw.Stop();
            result.HttpsSupported = true;
            result.HttpsStatusCode = (int)httpsResp.StatusCode;
            result.HttpsResponseMs = (int)httpsSw.ElapsedMilliseconds;
            result.FinalUrl = httpsResp.RequestMessage?.RequestUri?.ToString() ?? "";
            result.Html = await httpsResp.Content.ReadAsStringAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogInformation("HTTPS 检测失败 {Domain}: {Msg}", domain, ex.Message);
            result.HttpsSupported = false;

            // 2. HTTPS 失败，尝试 HTTP
            try
            {
                var httpSw = Stopwatch.StartNew();
                var httpResp = await _http.GetAsync($"http://{domain}", ct);
                httpSw.Stop();
                result.HttpStatusCode = (int)httpResp.StatusCode;
                result.HttpResponseMs = (int)httpSw.ElapsedMilliseconds;
                result.FinalUrl = httpResp.RequestMessage?.RequestUri?.ToString() ?? "";
                result.Html = await httpResp.Content.ReadAsStringAsync(ct);
            }
            catch (Exception ex2)
            {
                _logger.LogWarning("网站完全无法访问 {Domain}: {Msg}", domain, ex2.Message);
                result.Accessible = false;
                result.Error = ex2.Message;
                return result;
            }
        }

        result.Accessible = true;
        result.ResponseMs = result.HttpsResponseMs ?? result.HttpResponseMs ?? 0;

        // 3. 解析 HTML
        var html = result.Html ?? "";
        if (!string.IsNullOrEmpty(html))
        {
            result.Title = ExtractMeta(html, "title", tagMode: true);
            result.Description = ExtractMeta(html, "description");
            result.Keywords = ExtractMeta(html, "keywords");
            result.HasViewport = html.Contains("viewport", StringComparison.OrdinalIgnoreCase);
            result.HasFavicon = html.Contains("favicon", StringComparison.OrdinalIgnoreCase)
                || html.Contains("shortcut icon", StringComparison.OrdinalIgnoreCase)
                || html.Contains("apple-touch-icon", StringComparison.OrdinalIgnoreCase);
            result.ContentLength = html.Length;

            // 百度推广/统计代码检测（高价值客户标识）
            if (html.Contains("hm.baidu.com", StringComparison.OrdinalIgnoreCase))
            {
                result.HasBaiduAds = true;
                result.BaiduAdsType = "百度统计";
            }
            else if (html.Contains("bp.baidu.com", StringComparison.OrdinalIgnoreCase)
                || html.Contains("百度推广", StringComparison.OrdinalIgnoreCase))
            {
                result.HasBaiduAds = true;
                result.BaiduAdsType = "百度推广";
            }
            else if (html.Contains("lxbjs.baidu.com", StringComparison.OrdinalIgnoreCase)
                || html.Contains("百度商桥", StringComparison.OrdinalIgnoreCase)
                || html.Contains("qiao.baidu.com", StringComparison.OrdinalIgnoreCase))
            {
                result.HasBaiduAds = true;
                result.BaiduAdsType = "百度商桥";
            }
        }

        // 4. 汇总问题
        result.Issues = BuildIssues(result);
        return result;
    }

    /// <summary>
    /// 批量检测。
    /// </summary>
    public async Task<List<WebsiteCheckResult>> CheckBatchAsync(List<string> domains, CancellationToken ct = default)
    {
        var results = new List<WebsiteCheckResult>();
        // 并行检测（限制并发）
        var sem = new SemaphoreSlim(5);
        var tasks = domains.Select(async d =>
        {
            await sem.WaitAsync(ct);
            try
            {
                return await CheckAsync(d, ct);
            }
            finally
            {
                sem.Release();
            }
        });
        results.AddRange(await Task.WhenAll(tasks));
        return results;
    }

    private static string? ExtractMeta(string html, string name, bool tagMode = false)
    {
        try
        {
            if (tagMode)
            {
                var m = Regex.Match(html, @"<title[^>]*>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                return m.Success ? System.Net.WebUtility.HtmlDecode(m.Groups[1].Value.Trim()) : null;
            }
            // 匹配 <meta name="description" content="...">
            var pattern = $@"<meta[^>]+name=[""']{name}[""'][^>]+content=[""']([^""']*)[""']";
            var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                // 反向匹配：content 在前
                pattern = $@"<meta[^>]+content=[""']([^""']*)[""'][^>]+name=[""']{name}[""']";
                match = Regex.Match(html, pattern, RegexOptions.IgnoreCase);
            }
            return match.Success ? System.Net.WebUtility.HtmlDecode(match.Groups[1].Value.Trim()) : null;
        }
        catch
        {
            return null;
        }
    }

    private static List<string> BuildIssues(WebsiteCheckResult r)
    {
        var issues = new List<string>();
        if (!r.Accessible) { issues.Add("网站无法访问"); return issues; }
        if (!r.HttpsSupported) issues.Add("不支持HTTPS（浏览器提示不安全）");
        if (!r.HasViewport) issues.Add("无移动端viewport（手机浏览不适配）");
        if (string.IsNullOrEmpty(r.Title)) issues.Add("缺少页面标题");
        else if (r.Title.Length < 5) issues.Add("页面标题过短");
        if (string.IsNullOrEmpty(r.Description)) issues.Add("缺少meta描述");
        if (!r.HasFavicon) issues.Add("缺少网站图标");
        if (r.ResponseMs > 3000) issues.Add($"响应缓慢({r.ResponseMs}ms)");
        if (r.ContentLength < 500) issues.Add("页面内容过少");
        return issues;
    }
}

/// <summary>网站检测结果</summary>
public class WebsiteCheckResult
{
    public string Domain { get; set; } = "";
    public bool Accessible { get; set; }
    public bool HttpsSupported { get; set; }
    public int? HttpsStatusCode { get; set; }
    public int? HttpStatusCode { get; set; }
    public int? HttpsResponseMs { get; set; }
    public int? HttpResponseMs { get; set; }
    public int ResponseMs { get; set; }
    public string? FinalUrl { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Keywords { get; set; }
    public bool HasViewport { get; set; }
    public bool HasFavicon { get; set; }
    public int ContentLength { get; set; }
    public List<string> Issues { get; set; } = new();
    public string? Error { get; set; }

    /// <summary>是否检测到百度推广/统计代码（高价值客户标识）</summary>
    public bool HasBaiduAds { get; set; }
    /// <summary>检测到的百度推广代码类型（百度统计/百度商桥/百度推广）</summary>
    public string? BaiduAdsType { get; set; }

    /// <summary>HTML 原始内容（不返回给前端，内部用）</summary>
    public string? Html { get; set; }
}
