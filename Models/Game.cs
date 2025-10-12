using System.ComponentModel.DataAnnotations;

namespace SimpleAuthBasicApi.Models
{
    public class Game
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Genre { get; set; } = string.Empty;  // ประเภทเกม

        public string Description { get; set; } = string.Empty;

        public string ImagePath { get; set; } = string.Empty; // path รูปใน wwwroot/games

        public decimal Price { get; set; }

        public DateTime ReleaseDate { get; set; } = DateTime.Now; // วันที่วางขายอัตโนมัติ
    }
   
    public class UserLibraryItemDto
{
    public int GameId { get; set; }
    public string Title { get; set; } = "";
    public string Genre { get; set; } = "";
    public decimal PriceCurrent { get; set; }          // ราคาปัจจุบันในตาราง Games
    public string? ImagePath { get; set; }             // ชื่อไฟล์รูป/พาธ จาก Games
    public int TotalQty { get; set; }                  // จำนวนรวมที่เป็นเจ้าของ
    public DateTime LastPurchasedAt { get; set; }      // วันที่ซื้อครั้งล่าสุด
    public decimal TotalSpent { get; set; }            // เงินที่จ่ายไปกับเกมนี้ทั้งหมด (จาก PriceAtPurchase)
}

}
