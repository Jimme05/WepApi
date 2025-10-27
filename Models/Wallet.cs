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
    public int Qty { get; set; }
}
public sealed class PurchaseRequestDto
{
    public int UserId { get; set; }
    public List<PurchaseItemDto> Items { get; set; } = new();
    public string? CouponCode { get; set; } // ✅ เพิ่ม
}

 

}
