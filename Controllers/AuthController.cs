using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleAuthBasicApi.Data;
using SimpleAuthBasicApi.Models;

namespace SimpleAuthControllerApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    public AuthController(AppDbContext db) => _db = db;

   [HttpPost("register")]
public async Task<IActionResult> Register([FromForm] RegisterDto dto, IFormFile? profileImage)
{
    string? filePath = null;

    if (profileImage != null)
    {
        var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        if (!Directory.Exists(uploads))
            Directory.CreateDirectory(uploads);

        var fileName = Guid.NewGuid() + Path.GetExtension(profileImage.FileName);
        filePath = Path.Combine(uploads, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await profileImage.CopyToAsync(stream);
        }

        // เก็บ path เป็น URL
        filePath = $"/uploads/{fileName}";
    }

    // ตัวอย่างบันทึก user
    var user = new User
    {
        Name = dto.Name,
        Email = dto.Email,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
        AvatarUrl = filePath
    };

    _db.Users.Add(user);
    await _db.SaveChangesAsync();

    return Ok(new { message = "สมัครสมาชิกสำเร็จ", user });
}


    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password" });

        return Ok(new { id = user.Id, email = user.Email, role = user.Role });
    }

    // ไม่มี token/cookie => ทุกครั้งที่อยากได้ "ข้อมูลตัวเอง" ต้องส่ง email+password อีกครั้ง
    [HttpPost("me")]
    public async Task<IActionResult> Me([FromBody] LoginDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid credentials" });

        return Ok(new { id = user.Id, email = user.Email, role = user.Role });
    }
}
