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
            var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == req.UserId);
            if (wallet == null) return NotFound();

            var before = wallet.Balance;
            wallet.Balance += req.Amount;

            var trx = new Transaction
            {
                UserId = req.UserId,
                Type = "topup",
                Amount = req.Amount,
                Description = $"เติมเงินจำนวน {req.Amount}",
                BalanceBefore = before,
                BalanceAfter = wallet.Balance
            };

            _db.Transactions.Add(trx);
            await _db.SaveChangesAsync();
            return Ok(trx);
        }

        // ✅ ซื้อเกม
        [HttpPost("purchase")]
        public async Task<IActionResult> Purchase([FromBody] PurchaseRequestDto req, CancellationToken ct)
        {
            if (req == null || req.UserId <= 0)
                return BadRequest(new { message = "คำขอไม่ถูกต้อง" });

            if (req.Items == null || req.Items.Count == 0)
                return BadRequest(new { message = "ไม่มีรายการเกม" });

            var gameIds = req.Items.Select(i => i.GameId).ToList();
            var gamesInDb = await _db.Games
                .Where(g => gameIds.Contains(g.Id))
                .Select(g => new { g.Id, g.Title, g.Price })
                .ToListAsync(ct);

            var notFound = gameIds.Except(gamesInDb.Select(g => g.Id)).ToList();
            if (notFound.Any())
                return NotFound(new { message = $"ไม่พบเกมบางรายการ: {string.Join(", ", notFound)}" });

            decimal total = 0;
            var purchasedGames = new List<string>();

            foreach (var item in req.Items)
            {
                var g = gamesInDb.FirstOrDefault(x => x.Id == item.GameId);
                if (g == null) continue;

                total += g.Price * item.Qty;
                purchasedGames.Add($"{g.Title} x{item.Qty}");
            }

            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            await _db.Database.ExecuteSqlRawAsync(
                "SELECT 1 FROM Wallets WHERE UserId = {0} FOR UPDATE", req.UserId);

            var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == req.UserId, ct);
            if (wallet == null) return NotFound(new { message = "ไม่พบกระเป๋าเงิน" });

            if (wallet.Balance < total)
                return BadRequest(new { message = "ยอดเงินไม่พอ" });

            var before = wallet.Balance;
            wallet.Balance -= total;

            var trx = new Transaction
            {
                UserId = req.UserId,
                Type = "purchase",
                Amount = total,
                Description = $"ซื้อเกม {purchasedGames.Count} รายการ",
                BalanceBefore = before,
                BalanceAfter = wallet.Balance,
                CreatedAt = DateTime.UtcNow
            };

            _db.Transactions.Add(trx);

            // 🟢 เพิ่มเกมลง UserGames
            foreach (var item in req.Items)
            {
                var g = gamesInDb.FirstOrDefault(x => x.Id == item.GameId);
                if (g == null) continue;

                // ตรวจว่าซื้อเกมนี้ไปแล้วหรือยัง
                var existing = await _db.UserGames
                    .FirstOrDefaultAsync(ug => ug.UserId == req.UserId && ug.GameId == item.GameId, ct);

                if (existing != null)
                {
                    existing.Qty += item.Qty; // เพิ่มจำนวนถ้ามีอยู่แล้ว
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

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return Ok(new
            {
                message = "ชำระเงินสำเร็จ!",
                total,
                purchasedGames,
                balanceBefore = before,
                balanceAfter = wallet.Balance,
                transactionId = trx.Id
            });
        }





    }
}
