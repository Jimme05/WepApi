// Controllers/TransactionsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleAuthBasicApi.Data;
using SimpleAuthBasicApi.Models;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly AppDbContext _db;
    public TransactionsController(AppDbContext db) => _db = db;

    // GET: /api/Transactions/by-user?email=xxx@yyy.com
    // หรือ /api/Transactions/by-user?userId=123
    [HttpGet("by-user")]
    public async Task<IActionResult> GetByUser([FromQuery] string? email, [FromQuery] string? userId)
    {
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(userId))
            return BadRequest(new { message = "กรุณาส่ง email หรือ userId อย่างน้อยหนึ่งค่า" });

        var userQuery = _db.Users.AsQueryable();

        var user = !string.IsNullOrWhiteSpace(email)
            ? await userQuery.FirstOrDefaultAsync(u => u.Email == email)
            : await userQuery.FirstOrDefaultAsync(u => u.Id == int.Parse(userId!));

        if (user == null) return NotFound(new { message = "ไม่พบผู้ใช้" });

        var list = await _db.Transactions
            .Where(t => t.UserId == user.Id)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new Transaction
            {
                Id = t.Id,
                UserId = t.UserId,
                Email = user.Email,
                Type = t.Type,                 // "topup" | "purchase"
                Amount = t.Amount,
                BalanceBefore = t.BalanceBefore,
                BalanceAfter = t.BalanceAfter,
                Description = t.Description,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        return Ok(list);
    }

    // (ออปชัน) ให้ Admin ได้รายชื่อผู้ใช้ไปขึ้น dropdown
    // GET: /api/Transactions/users-min
    [HttpGet("users-min")]
    public async Task<IActionResult> GetUsersMin()
    {
        var users = await _db.Users
            .OrderBy(u => u.Email)
            .Select(u => new { u.Id, u.Name, u.Email })
            .ToListAsync();

        return Ok(users);
    }
}
