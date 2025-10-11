// Models/Dtos/TransactionDto.cs
public class Transaction
{
    public int Id { get; set; }
    public int UserId { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Type { get; set; } = default!;  // "topup" | "purchase"
    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}
