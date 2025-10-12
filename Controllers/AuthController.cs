using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleAuthBasicApi.Data;
using SimpleAuthBasicApi.Models;
using System.Text.RegularExpressions;

namespace SimpleAuthControllerApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    public AuthController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }


    [HttpPost("register")]
public async Task<IActionResult> Register([FromForm] RegisterDto dto)
{
    string? savedFileName = null;

    // ถ้ามีรูปโปรไฟล์แนบมาด้วย
    if (dto.ProfileImage != null && dto.ProfileImage.Length > 0)
    {
        using var httpClient = new HttpClient();

        var content = new MultipartFormDataContent();
        var fileStream = dto.ProfileImage.OpenReadStream();
        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(dto.ProfileImage.ContentType);

        // ส่งไปยัง server 203
        content.Add(fileContent, "file", dto.ProfileImage.FileName);

        var uploadResponse = await httpClient.PostAsync("http://202.28.34.203:30000/upload", content);

        if (!uploadResponse.IsSuccessStatusCode)
        {
            return BadRequest(new { message = "อัปโหลดรูปไม่สำเร็จ" });
        }

        var jsonString = await uploadResponse.Content.ReadAsStringAsync();
        var json = System.Text.Json.JsonDocument.Parse(jsonString);

        // รับชื่อไฟล์จาก JSON ที่ฝั่ง 203 ส่งกลับ เช่น { "fileName": "abc.jpg" }
        savedFileName =
            json.RootElement.TryGetProperty("fileName", out var fn) ? fn.GetString() :
            json.RootElement.TryGetProperty("filename", out var fn2) ? fn2.GetString() :
            json.RootElement.TryGetProperty("path", out var fn3) ? fn3.GetString() :
            null;
    }

    // ✅ สร้าง user ใหม่
    var user = new User
    {
        Name = dto.Name,
        Email = dto.Email,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
        AvatarUrl = savedFileName // เก็บเฉพาะชื่อไฟล์ เช่น "abc.jpg"
    };

    _db.Users.Add(user);
    await _db.SaveChangesAsync();

    return Ok(new
    {
        user.Id,
        user.Email,
        user.AvatarUrl,
        profileUrl = savedFileName != null ? $"http://202.28.34.203:30000/upload/{savedFileName}" : null
    });
}





    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password" });

        return Ok(new { id = user.Id, email = user.Email, role = user.Role, name = user.Name });
    }

    // ไม่มี token/cookie => ทุกครั้งที่อยากได้ "ข้อมูลตัวเอง" ต้องส่ง email+password อีกครั้ง

    [HttpPost("me")]
    public async Task<IActionResult> Me([FromBody] EmailDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            return BadRequest(new { message = "Email is required." });

        var email = dto.Email.Trim().ToLower();

        var user = await _db.Users
            .AsNoTracking()
            //.Include(u => u.OwnedGames)         // ถ้ามีตารางลูก
            //.Include(u => u.Wallet)             // ถ้ามี wallet แยกตาราง
            .SingleOrDefaultAsync(u => u.Email.ToLower() == email);

        if (user == null) return NotFound(new { message = "User not found." });

        // ✅ อย่าส่งรหัสผ่าน/แฮชกลับ
        var result = new
        {
            id = user.Id,
            name = user.Name,             // หรือ Username
            email = user.Email,
            role = user.Role,
            profileImage = user.AvatarUrl,
            createdAt = user.CreatedAt,
            // ownedGames = user.OwnedGames.Select(g => new { g.Id, g.Title }) // ถ้ามี
        };

        return Ok(result);
    }



   
    
    [HttpPut("updateByEmail")]
public async Task<IActionResult> UpdateByEmail([FromForm] UpdateUserByEmailDto dto, IFormFile? profileImage)
{
    var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
    if (user == null)
        return NotFound(new { message = "User not found" });

    user.Name = dto.Name ?? user.Name;

    if (!string.IsNullOrEmpty(dto.Password))
    {
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
    }

    if (profileImage != null)
    {
        var fileName = $"{Guid.NewGuid()}_{profileImage.FileName}";
        var filePath = Path.Combine(_env.WebRootPath, "profile", fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await profileImage.CopyToAsync(stream);
        }

        user.AvatarUrl = fileName;
    }

    await _db.SaveChangesAsync();

    return Ok(new
    {
        user.Id,
        user.Name,
        user.Email,
        user.AvatarUrl,
        user.Role
    });
}

}
