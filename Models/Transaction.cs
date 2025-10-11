using System.ComponentModel.DataAnnotations;

namespace SimpleAuthBasicApi.Models
{
    public class Transaction
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        public string Type { get; set; } = ""; // topup, purchase

        public decimal Amount { get; set; }

        public string Description { get; set; } = "";

        public decimal BalanceBefore { get; set; }

        public decimal BalanceAfter { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
