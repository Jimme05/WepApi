using Microsoft.EntityFrameworkCore;
using SimpleAuthBasicApi.Models;

namespace SimpleAuthBasicApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
            e.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(255).IsRequired();
            e.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(100);
            e.Property(x => x.AvatarUrl).HasColumnName("avatar_url").HasMaxLength(255);
            e.Property(x => x.Role).HasColumnName("role").HasMaxLength(10).HasDefaultValue("User");
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.HasIndex(x => x.Email).IsUnique();
        });
    }
}
