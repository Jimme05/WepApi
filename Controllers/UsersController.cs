using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleAuthBasicApi.Data;
using System.Security.Claims;

namespace SimpleAuthBasicApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;
    public UsersController(AppDbContext db) => _db = db;

    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = "Basic")]
    public async Task<IActionResult> Me()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idStr, out var id)) return Unauthorized();

        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();
        return Ok(new { user.Id, user.Email, user.DisplayName, user.Role });
    }

    [HttpGet]
    [Authorize(AuthenticationSchemes = "Basic", Roles = "Admin")]
    public async Task<IActionResult> ListUsers()
    {
        var users = await _db.Users
            .Select(u => new { u.Id, u.Email, u.Name, u.DisplayName, u.Role, u.CreatedAt })
            .ToListAsync();
        return Ok(users);
    }
}
