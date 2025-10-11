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
        public async Task<IActionResult> Purchase([FromBody] Transaction req)
        {
            var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == req.UserId);
            if (wallet == null) return NotFound();

            if (wallet.Balance < req.Amount)
                return BadRequest(new { message = "ยอดเงินไม่พอ" });

            var before = wallet.Balance;
            wallet.Balance -= req.Amount;

            var trx = new Transaction
            {
                UserId = req.UserId,
                Type = "purchase",
                Amount = req.Amount,
                Description = $"ซื้อเกม {req.Description}",
                BalanceBefore = before,
                BalanceAfter = wallet.Balance
            };

            _db.Transactions.Add(trx);
            await _db.SaveChangesAsync();
            return Ok(trx);
        }

        // ✅ ดูประวัติการทำรายการทั้งหมด (Admin)
        [HttpGet("history")]
        public async Task<IActionResult> GetAllTransactions()
        {
            return Ok(await _db.Transactions.OrderByDescending(t => t.CreatedAt).ToListAsync());
        }
    }
}
