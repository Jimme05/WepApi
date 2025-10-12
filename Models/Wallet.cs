using System.ComponentModel.DataAnnotations;

namespace SimpleAuthBasicApi.Models
{
    public class Wallet
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        public decimal Balance { get; set; } = 0;
    }
    public sealed class PurchaseItemDto
    {
        public int GameId { get; set; }
        public int Qty { get; set; } = 1;
        public decimal Price { get; set; } // ราคาต่อชิ้น
    }

    public sealed class PurchaseRequestDto
    {
        public int UserId { get; set; }
        public decimal Amount { get; set; }            // ราคารวมที่ฝั่ง client คำนวณมา
        public string? Description { get; set; }       // ข้อความเพิ่มเติม
        public List<PurchaseItemDto> Items { get; set; } = new();
    }
}
