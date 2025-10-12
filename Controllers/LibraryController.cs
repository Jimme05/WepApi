using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleAuthBasicApi.Data;
using SimpleAuthBasicApi.Models;

[ApiController]
[Route("api/[controller]")]
public class LibraryController : ControllerBase
{
    private readonly AppDbContext _db;
    public LibraryController(AppDbContext db) => _db = db;

    /// <summary>
    /// ดึงคลังเกมของผู้ใช้ตาม userId
    /// </summary>
    [HttpGet("{userId:int}")]
    public async Task<IActionResult> GetByUserId(int userId, CancellationToken ct)
    {
        // มี user จริงไหม (กันยิงมั่ว)
        var hasUser = await _db.Users.AnyAsync(u => u.Id == userId, ct);
        if (!hasUser) return NotFound(new { message = "User not found" });

        // group รายการซื้อ แล้ว join กับ Games เพื่อเอาข้อมูลเกม
        var list = await _db.UserGames
            .Where(x => x.UserId == userId)
            .GroupBy(x => x.GameId)
            .Select(g => new
            {
                GameId = g.Key,
                TotalQty = g.Sum(x => x.Qty),
                LastPurchasedAt = g.Max(x => x.PurchasedAt),
                TotalSpent = g.Sum(x => x.Qty * x.PriceAtPurchase)
            })
            .Join(_db.Games,
                  a => a.GameId,
                  game => game.Id,
                  (a, game) => new UserLibraryItemDto
                  {
                      GameId = game.Id,
                      Title = game.Title,
                      Genre = game.Genre,
                      PriceCurrent = game.Price,
                      ImagePath = game.ImagePath,     // เก็บชื่อไฟล์ไว้ ใช้กับ 203:30000/upload/<ชื่อไฟล์>
                      TotalQty = a.TotalQty,
                      LastPurchasedAt = a.LastPurchasedAt,
                      TotalSpent = a.TotalSpent
                  })
            .OrderByDescending(x => x.LastPurchasedAt)
            .ToListAsync(ct);

        return Ok(list);
    }

    /// <summary>
    /// ดึงคลังเกมของผู้ใช้ด้วยอีเมล (สะดวกฝั่ง Front)
    /// GET /api/Library/by-email?email=xxx@yyy.com
    /// </summary>
    [HttpGet("by-email")]
    public async Task<IActionResult> GetByEmail([FromQuery] string email, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { message = "email is required" });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user == null) return NotFound(new { message = "User not found" });

        return await GetByUserId(user.Id, ct);
    }

    /// <summary>
    /// ตรวจว่า user เป็นเจ้าของเกมนี้หรือยัง
    /// </summary>
    [HttpGet("has/{userId:int}/{gameId:int}")]
    public async Task<IActionResult> HasGame(int userId, int gameId, CancellationToken ct)
    {
        var owned = await _db.UserGames.AnyAsync(x => x.UserId == userId && x.GameId == gameId, ct);
        return Ok(new { owned });
    }
}
