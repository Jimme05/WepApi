using System.ComponentModel.DataAnnotations;

namespace SimpleAuthBasicApi.Models;

public class User
{
    public int Id { get; set; }
    [MaxLength(100)] public required string Name { get; set; }
    [MaxLength(255)] public required string Email { get; set; }
    [MaxLength(255)] public required string PasswordHash { get; set; }
    [MaxLength(100)] public string? DisplayName { get; set; }
    [MaxLength(255)] public string? AvatarUrl { get; set; }
    [MaxLength(10)] public string Role { get; set; } = "User";
    public DateTime CreatedAt { get; set; }
}

public class RegisterDto
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    
    public IFormFile? ProfileImage { get; set; }
}
public record LoginDto(string Email, string Password); // for Swagger try-out convenience
public class UpdateUserByEmailDto
{
    public string Email { get; set; } = string.Empty; // email เดิม
    public string? Name { get; set; }
    public string? Password { get; set; }
}


    // สิ่งที่คืนกลับไปหา client
    public class UserResponseDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string Role { get; set; } = "User";
        // เก็บ “ชื่อไฟล์” ใน DB แต่โชว์ “URL เสิร์ฟรูป” ให้ client
        public string? ProfileImageUrl { get; set; }
    }

public class EmailDto
{
    public string Email { get; set; } = string.Empty;
}
public class UserGame
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public int GameId { get; set; }
    public int Qty { get; set; } = 1;

    public decimal PriceAtPurchase { get; set; }
    public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;

    // (ถ้ามี relation)
    public User? User { get; set; }
    public Game? Game { get; set; }
}

