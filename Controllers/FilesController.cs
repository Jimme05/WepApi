using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ImagesController : ControllerBase
{
    private readonly IHttpClientFactory _http;
    private readonly string _origin = Environment.GetEnvironmentVariable("SOURCE_ORIGIN")
                                      ?? "http://202.28.34.203:30000";
    public ImagesController(IHttpClientFactory http) => _http = http;

    // GET /api/images/proxy/upload/{*path}
    [HttpGet("proxy/upload/{*path}")]
    public async Task<IActionResult> ProxyUpload(string path, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Contains(".."))
            return BadRequest("invalid path");

        var upstreamUrl = $"{_origin}/upload/{path}";
        var client = _http.CreateClient("img-proxy");

        var resp = await client.GetAsync(upstreamUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!resp.IsSuccessStatusCode)
            return StatusCode((int)resp.StatusCode, $"Upstream error {resp.StatusCode}");

        var contentType = resp.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
        var cache = resp.Headers.CacheControl?.ToString() ?? "public, max-age=604800, immutable";

        var bytes = await resp.Content.ReadAsByteArrayAsync(ct);

        Response.Headers["Cache-Control"] = cache;
        Response.ContentType = contentType;
        Response.ContentLength = bytes.LongLength; // <-- สำคัญบนบางแพลตฟอร์ม
        Response.Headers["X-Proxy-Source"] = upstreamUrl;

        return File(bytes, contentType);
    }

    [HttpGet("proxy/debug/{*path}")]
public async Task<IActionResult> DebugFetch(string path, CancellationToken ct)
{
    var url = $"{_origin}/upload/{path}";
    var client = _http.CreateClient("img-proxy");
    try
    {
        var sw = Stopwatch.StartNew();
        var resp = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        var len = resp.Content.Headers.ContentLength;
        var ctType = resp.Content.Headers.ContentType?.ToString();
        var first256 = await resp.Content.ReadAsByteArrayAsync(ct);
        sw.Stop();

        return Ok(new {
            upstream = url,
            status = (int)resp.StatusCode,
            contentType = ctType,
            contentLengthHeader = len,
            actuallyRead = first256?.Length,
            elapsedMs = sw.ElapsedMilliseconds
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { upstream = url, error = ex.Message });
    }
}
}
