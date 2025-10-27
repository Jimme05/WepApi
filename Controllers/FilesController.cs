using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ImagesController : ControllerBase
{
    private readonly IHttpClientFactory _http;

    // ปรับเป็น env var หรือค่า config ได้
    private readonly string _origin = Environment.GetEnvironmentVariable("SOURCE_ORIGIN")
                                      ?? "http://202.28.34.203:30000";

    public ImagesController(IHttpClientFactory http)
    {
        _http = http;
    }

    // GET: /api/Images/proxy/upload/{*path}
    [HttpGet("proxy/upload/{*path}")]
    public async Task<IActionResult> ProxyUpload([FromRoute] string path, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(path))
            return BadRequest("path is required.");

        // กัน path traversal เล็กน้อย
        if (path.Contains("..")) return BadRequest("invalid path");

        var upstreamUrl = $"{_origin}/upload/{path}";
        var client = _http.CreateClient();

        using var upstream = await client.GetAsync(upstreamUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!upstream.IsSuccessStatusCode)
            return StatusCode((int)upstream.StatusCode, $"Upstream error {upstream.StatusCode}");

        var contentType = upstream.Content.Headers.ContentType?.ToString()
                          ?? "application/octet-stream";
        var cacheControl = upstream.Headers.CacheControl?.ToString()
                          ?? "public, max-age=604800, immutable"; // 7 วัน

        var stream = await upstream.Content.ReadAsStreamAsync(ct);
        Response.Headers["Cache-Control"] = cacheControl;
        return File(stream, contentType); // stream ผ่าน HTTPS ของ Render
    }
}
