using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/proxy")]
public class ProxyController : ControllerBase
{
    private readonly IHttpClientFactory _factory;
    public ProxyController(IHttpClientFactory factory) => _factory = factory;

    // GET /api/proxy/image/{fileName}
    [HttpGet("image/{fileName}")]
    public async Task<IActionResult> Image(string fileName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return BadRequest(new { message = "fileName required" });

        // กัน path traversal, รับแค่ชื่อไฟล์
        fileName = Path.GetFileName(fileName);

        var client = _factory.CreateClient("ImageOrigin");
        var upstreamUrl = $"upload/{Uri.EscapeDataString(fileName)}";

        var resp = await client.GetAsync(upstreamUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!resp.IsSuccessStatusCode) return NotFound();

        var contentType = resp.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
        var stream = await resp.Content.ReadAsStreamAsync(ct);

        // แคช 1 วัน (ปรับตามต้องการ)
        Response.Headers["Cache-Control"] = "public, max-age=86400";
        return File(stream, contentType);
    }
}
