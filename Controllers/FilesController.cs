using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ImagesController : ControllerBase
{
    private readonly IHttpClientFactory _http;
    private readonly string _origin = Environment.GetEnvironmentVariable("SOURCE_ORIGIN")
                                      ?? "http://202.28.34.203:30000";

    public ImagesController(IHttpClientFactory http) => _http = http;

    // GET: /api/images/proxy/upload/{*path}
    [HttpGet("proxy/upload/{*path}")]
    public async Task<IActionResult> ProxyUpload(string path, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Contains(".."))
            return BadRequest("invalid path");

        var upstreamUrl = $"{_origin}/upload/{path}";
        var client = _http.CreateClient();

        // อย่าพันด้วย using เพื่อไม่ให้ถูก dispose ก่อนเวลา
        var resp = await client.GetAsync(upstreamUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!resp.IsSuccessStatusCode)
            return StatusCode((int)resp.StatusCode, $"Upstream error {resp.StatusCode}");

        var contentType = resp.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
        var cache = resp.Headers.CacheControl?.ToString() ?? "public, max-age=604800, immutable";

        var bytes = await resp.Content.ReadAsByteArrayAsync(ct);
        Response.Headers["Cache-Control"] = cache;
        return File(bytes, contentType);
    }
}
