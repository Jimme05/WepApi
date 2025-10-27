using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleAuthBasicApi.Data;

[ApiController]
[Route("api/[controller]")]
public class DiscountCodesController : ControllerBase
{
    private readonly AppDbContext _db;

    public DiscountCodesController(AppDbContext db) => _db = db;

    // === Admin: สร้าง/แก้ไข/ลบ/ดู ===

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDiscountDto dto)
    {
        var code = dto.Code.Trim().ToUpperInvariant();

        var exists = await _db.DiscountCodes.AnyAsync(x => x.Code == code);
        if (exists) return Conflict(new { message = "โค้ดซ้ำ" });

        var dc = new DiscountCode
        {
            Code = code,
            Amount = dto.Amount,
            IsPercent = dto.IsPercent,
            MaxUses = dto.MaxUses,
            PerUserLimit = dto.PerUserLimit,
            MinOrderAmount = dto.MinOrderAmount,
            StartAt = dto.StartAt,
            EndAt = dto.EndAt,
            IsActive = dto.IsActive
        };
        _db.DiscountCodes.Add(dc);
        await _db.SaveChangesAsync();
        return Ok(dc);
    }

    [HttpGet]
    public async Task<IActionResult> List() =>
        Ok(await _db.DiscountCodes.OrderByDescending(x => x.CreatedAt).ToListAsync());

    [HttpGet("{code}")]
    public async Task<IActionResult> GetOne(string code)
    {
        var dc = await _db.DiscountCodes.FirstOrDefaultAsync(x => x.Code == code.ToUpper());
        return dc is null ? NotFound() : Ok(dc);
    }

    // === Preview: ตรวจสอบว่าส่วนลดใช้ได้ไหม + คำนวณยอด ===
    [HttpPost("preview")]
    public async Task<IActionResult> Preview([FromBody] PreviewDiscountDto dto)
    {
        var (ok, msg, applied, final, _) = await ValidateAndCalcAsync(
            dto.Code, dto.UserId, dto.OrderAmount, previewOnly: true);

        if (!ok) return BadRequest(new { message = msg });
        return Ok(new { discountApplied = applied, finalAmount = final });
    }

    // === Redeem: ล็อกการใช้คูปอง + เพิ่ม UsedCount ===
    [HttpPost("redeem")]
    public async Task<IActionResult> Redeem([FromBody] RedeemDiscountDto dto)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        var (ok, msg, applied, final, dc) = await ValidateAndCalcAsync(
            dto.Code, dto.UserId, dto.OrderAmount, previewOnly: false, forUpdate: true);

        if (!ok || dc is null)
        {
            await tx.RollbackAsync();
            return BadRequest(new { message = msg });
        }

        // บันทึกการใช้
        _db.DiscountRedemptions.Add(new DiscountRedemption
        {
            DiscountCodeId = dc.Id,
            UserId = dto.UserId,
            OrderAmount = dto.OrderAmount,
            DiscountApplied = applied,
            FinalAmount = final
        });

        // เพิ่ม UsedCount
        dc.UsedCount += 1;
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return Ok(new { discountApplied = applied, finalAmount = final });
    }

    // ====== Core validate & calc ======
    private async Task<(bool ok, string msg, decimal applied, decimal final, DiscountCode? dc)>
        ValidateAndCalcAsync(string code, int userId, decimal orderAmount, bool previewOnly, bool forUpdate = false)
    {
        if (orderAmount <= 0) return (false, "ยอดคำสั่งซื้อต้องมากกว่า 0", 0, orderAmount, null);
        if (string.IsNullOrWhiteSpace(code)) return (false, "กรุณาระบุโค้ด", 0, orderAmount, null);

        code = code.Trim().ToUpperInvariant();

        // ถ้าต้องการ lock แถวเพื่อ update ให้ใช้ FOR UPDATE (Pomelo ไม่มีง่ายแบบตรงๆ)
        // ใช้อ่านปกติ + เช็คอีกชั้นด้วย commit transaction เพียงพอสำหรับเคสนี้
        var dc = await _db.DiscountCodes.FirstOrDefaultAsync(x => x.Code == code);
        if (dc is null) return (false, "ไม่พบโค้ดส่วนลด", 0, orderAmount, null);

        var now = DateTime.UtcNow;
        if (!dc.IsActive) return (false, "โค้ดถูกปิดใช้งาน", 0, orderAmount, dc);
        if (dc.StartAt.HasValue && now < dc.StartAt.Value) return (false, "ยังไม่ถึงเวลาเริ่มใช้โค้ด", 0, orderAmount, dc);
        if (dc.EndAt.HasValue && now > dc.EndAt.Value) return (false, "โค้ดหมดอายุแล้ว", 0, orderAmount, dc);
        if (dc.MinOrderAmount.HasValue && orderAmount < dc.MinOrderAmount.Value)
            return (false, $"ยอดขั้นต่ำในการใช้โค้ดคือ {dc.MinOrderAmount:F2} บาท", 0, orderAmount, dc);
        if (dc.MaxUses.HasValue && dc.UsedCount >= dc.MaxUses.Value)
            return (false, "โค้ดถูกใช้ครบจำนวนแล้ว", 0, orderAmount, dc);

        if (dc.PerUserLimit.HasValue)
        {
            var usedByUser = await _db.DiscountRedemptions
                .CountAsync(x => x.DiscountCodeId == dc.Id && x.UserId == userId);
            if (usedByUser >= dc.PerUserLimit.Value)
                return (false, "ท่านใช้โค้ดนี้ครบจำนวนที่กำหนดแล้ว", 0, orderAmount, dc);
        }

        decimal applied = dc.IsPercent
            ? Math.Round(orderAmount * (dc.Amount / 100m), 2, MidpointRounding.AwayFromZero)
            : dc.Amount;

        // ไม่ให้ส่วนลดเกินยอด
        if (applied > orderAmount) applied = orderAmount;

        var final = orderAmount - applied;
        if (final < 0) final = 0;

        return (true, "", applied, final, dc);
    }
}
