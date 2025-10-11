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
}
