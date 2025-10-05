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

public record RegisterDto(string Name, string Email, string Password, string? AvatarUrl);
public record LoginDto(string Email, string Password); // for Swagger try-out convenience
public record UpdateProfileDto(string? Name, string? DisplayName);
