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
}
