using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleAuthBasicApi.Data;
using SimpleAuthBasicApi.Models;

namespace SimpleAuthBasicApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WalletController : ControllerBase
    {
        private readonly AppDbContext _db;
        public WalletController(AppDbContext db) { _db = db; }

        // ✅ ดูยอดเงิน
        [HttpGet("{userId:int}")]
        public async Task<IActionResult> GetWallet(int userId)
        {
            var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet == null)
            {
                wallet = new Wallet { UserId = userId, Balance = 0 };
                _db.Wallets.Add(wallet);
                await _db.SaveChangesAsync();
            }
            return Ok(wallet);
        }

        // ✅ เติมเงิน
        [HttpPost("topup")]
        public async Task<IActionResult> TopUp([FromBody] Transaction req)
        {
            if (req.UserId == 0)
                return BadRequest(new { message = "UserId ต้องไม่เป็นค่าว่าง" });

            // ✅ ค้นหา wallet ของ user
            var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == req.UserId);

            // ✅ ถ้ายังไม่มี wallet — สร้างใหม่ให้เลย
            if (wallet == null)
            {
                wallet = new Wallet
                {
                    UserId = req.UserId,
                    Balance = 0,

                };

                _db.Wallets.Add(wallet);
                await _db.SaveChangesAsync(); // ต้อง save ก่อนเพื่อให้ Wallet.Id ถูกสร้าง
            }

            // ✅ ทำการเติมเงิน
            var before = wallet.Balance;
            wallet.Balance += req.Amount;

            // ✅ บันทึก Transaction
            var trx = new Transaction
            {
                UserId = req.UserId,
                Type = "topup",
                Amount = req.Amount,
                Description = $"เติมเงินจำนวน {req.Amount} บาท",
                BalanceBefore = before,
                BalanceAfter = wallet.Balance,
                CreatedAt = DateTime.UtcNow
            };

            _db.Transactions.Add(trx);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "เติมเงินสำเร็จ",
                wallet.Balance,
                trx.Id,
                trx.Description,
                trx.CreatedAt
            });
        }


        // ✅ ซื้อเกม
        // ✅ ซื้อเกม
        [HttpPost("purchase")]
        public async Task<IActionResult> Purchase([FromBody] PurchaseRequestDto req, CancellationToken ct)
        {
            if (req is null || req.UserId <= 0)
                return BadRequest(new { message = "คำขอไม่ถูกต้อง" });
            if (req.Items is null || req.Items.Count == 0)
                return BadRequest(new { message = "ไม่มีรายการเกม" });

            var gameIds = req.Items.Select(i => i.GameId).ToList();

            var gamesInDb = await _db.Games
                .Where(g => gameIds.Contains(g.Id))
                .Select(g => new { g.Id, g.Title, g.Price })
                .ToListAsync(ct);

            var notFound = gameIds.Except(gamesInDb.Select(g => g.Id)).ToList();
            if (notFound.Any())
                return NotFound(new { message = $"ไม่พบเกมบางรายการ: {string.Join(", ", notFound)}" });

            // ===== 1) คำนวณยอดก่อนใช้คูปอง =====
            decimal subtotal = 0;
            var purchasedGames = new List<string>();
            foreach (var item in req.Items)
            {
                var g = gamesInDb.FirstOrDefault(x => x.Id == item.GameId);
                if (g is null) continue;
                var line = g.Price * item.Qty;
                subtotal += line;
                purchasedGames.Add($"{g.Title} x{item.Qty}");
            }

            // ===== 2) ส่วนลดเริ่มต้น =====
            decimal discountAmount = 0m;
            DiscountCode? coupon = null;

            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            // ===== 3) ตรวจคูปองถ้ามี =====
            if (!string.IsNullOrWhiteSpace(req.CouponCode))
            {
                coupon = await _db.DiscountCodes
                    .Where(c => c.Code == req.CouponCode)
                    .FirstOrDefaultAsync(ct);

                if (coupon is null)
                    return BadRequest(new { message = "คูปองไม่ถูกต้อง" });

                // ✅ ตรวจว่าผู้ใช้นี้เคยใช้คูปองนี้แล้วหรือยัง (เช็กจาก Transaction)
                bool usedAlready = await _db.Transactions
                    .AnyAsync(t => t.UserId == req.UserId &&
                                   t.Type == "purchase" &&
                                   t.Description.Contains(req.CouponCode), ct);

                if (usedAlready)
                    return BadRequest(new { message = "คุณได้ใช้คูปองนี้ไปแล้ว" });

                // ✅ ตรวจสถานะและเงื่อนไขทั่วไป
                var now = DateTime.UtcNow;
                if (!coupon.IsActive)
                    return BadRequest(new { message = "คูปองถูกปิดการใช้งาน" });
                if (coupon.StartAt.HasValue && now < coupon.StartAt.Value)
                    return BadRequest(new { message = "คูปองยังไม่เริ่มใช้งาน" });
                if (coupon.EndAt.HasValue && now > coupon.EndAt.Value)
                    return BadRequest(new { message = "คูปองหมดอายุแล้ว" });
                if (coupon.MinOrderAmount.HasValue && subtotal < coupon.MinOrderAmount.Value)
                    return BadRequest(new { message = $"ยอดขั้นต่ำ {coupon.MinOrderAmount.Value} บาท" });
                if (coupon.MaxUses.HasValue && coupon.UsedCount >= coupon.MaxUses.Value)
                    return BadRequest(new { message = "คูปองถูกใช้ครบจำนวนแล้ว" });

                // ✅ คำนวณส่วนลด
                if (coupon.IsPercent)
                {
                    discountAmount = Math.Round(subtotal * (coupon.Amount / 100m), 2, MidpointRounding.AwayFromZero);
                }
                else
                {
                    discountAmount = coupon.Amount;
                }

                if (discountAmount < 0) discountAmount = 0;
                if (discountAmount > subtotal) discountAmount = subtotal;
            }

            // ===== 4) ยอดสุทธิ =====
            var total = subtotal - discountAmount;

            // ===== 5) ล็อก wallet ของ user =====
            await _db.Database.ExecuteSqlRawAsync(
                "SELECT 1 FROM Wallets WHERE UserId = {0} FOR UPDATE", req.UserId);

            var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == req.UserId, ct);
            if (wallet is null)
                return NotFound(new { message = "ไม่พบกระเป๋าเงิน" });

            if (wallet.Balance < total)
                return BadRequest(new { message = "ยอดเงินไม่พอ" });

            var before = wallet.Balance;
            wallet.Balance -= total;

            // ===== 6) บันทึก Transaction =====
            var desc = discountAmount > 0
                ? $"ซื้อเกม {purchasedGames.Count} รายการ (ใช้คูปอง {req.CouponCode}, ลด {discountAmount:0.##})"
                : $"ซื้อเกม {purchasedGames.Count} รายการ";

            var trx = new Transaction
            {
                UserId = req.UserId,
                Type = "purchase",
                Amount = total,
                Description = desc,
                BalanceBefore = before,
                BalanceAfter = wallet.Balance,
                CreatedAt = DateTime.UtcNow
            };
            _db.Transactions.Add(trx);

            // ===== 7) เพิ่มเกมในคลังผู้ใช้ =====
            foreach (var item in req.Items)
            {
                var g = gamesInDb.First(x => x.Id == item.GameId);

                var existing = await _db.UserGames
                    .FirstOrDefaultAsync(ug => ug.UserId == req.UserId && ug.GameId == item.GameId, ct);

                if (existing != null)
                {
                    existing.Qty += item.Qty;
                }
                else
                {
                    _db.UserGames.Add(new UserGame
                    {
                        UserId = req.UserId,
                        GameId = item.GameId,
                        Qty = item.Qty,
                        PriceAtPurchase = g.Price,
                        PurchasedAt = DateTime.UtcNow
                    });
                }
            }

            // ===== 8) อัปเดตจำนวนคูปอง =====
            if (coupon != null)
            {
                coupon.UsedCount += 1;

                // ปิดคูปองอัตโนมัติถ้าใช้ครบ
                if (coupon.MaxUses.HasValue && coupon.UsedCount >= coupon.MaxUses.Value)
                    coupon.IsActive = false;
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return Ok(new
            {
                message = "ชำระเงินสำเร็จ!",
                subtotal,
                discount = discountAmount,
                total,
                coupon = coupon?.Code,
                purchasedGames,
                balanceBefore = before,
                balanceAfter = wallet.Balance,
                transactionId = trx.Id
            });
        }






    }
}
