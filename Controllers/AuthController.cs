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
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            return BadRequest(new { message = "Email already registered" });

        var hash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        var user = new User { Email = dto.Email, PasswordHash = hash, Name = dto.Name, AvatarUrl = dto.AvatarUrl };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Registered successfully" });
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
