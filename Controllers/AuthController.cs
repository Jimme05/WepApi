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
    if (string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.Password))
        return BadRequest(new { message = "ข้อมูลไม่ครบ" });

    string fileName = "default.png";
    if (profileImage != null)
    {
        // สร้างชื่อไฟล์ไม่ให้ซ้ำ
        fileName = $"{Guid.NewGuid()}_{profileImage.FileName}";
        var savePath = Path.Combine("wwwroot/profile", fileName);
        using (var stream = new FileStream(savePath, FileMode.Create))
        {
            await profileImage.CopyToAsync(stream);
        }
    }

    var user = new User {
        Name = dto.Name,
        Email = dto.Email,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
        AvatarUrl = fileName,
        Role = "User"
    };

    _db.Users.Add(user);
    await _db.SaveChangesAsync();

    return Ok(new { id = user.Id, email = user.Email, role = user.Role, profileImage = fileName });
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


    
    [HttpPut("update/{id}")]
public async Task<IActionResult> UpdateUser(int id, [FromForm] UpdateUserDto dto, IFormFile? profileImage)
{
    var user = await _db.Users.FindAsync(id);
    if (user == null)
        return NotFound(new { message = "User not found" });

    // อัปเดตข้อมูลพื้นฐาน
    user.Name = dto.Name ?? user.Name;
    user.Email = dto.Email ?? user.Email;

    if (!string.IsNullOrEmpty(dto.Password))
    {
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
    }

    // อัปโหลดไฟล์ใหม่ (ถ้ามี)
    if (profileImage != null)
    {
        var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        if (!Directory.Exists(uploads))
            Directory.CreateDirectory(uploads);

        var fileName = Guid.NewGuid() + Path.GetExtension(profileImage.FileName);
        var filePath = Path.Combine(uploads, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await profileImage.CopyToAsync(stream);
        }

        user.AvatarUrl = $"/uploads/{fileName}";
    }

    _db.Users.Update(user);
    await _db.SaveChangesAsync();

    return Ok(new { message = "User updated successfully", user });
}

}
