using Microsoft.EntityFrameworkCore;
using VirtualTryOn.Api.Models;

namespace VirtualTryOn.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(user => user.Id);

            entity.Property(user => user.FullName).HasMaxLength(120).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(254).IsRequired();
            entity.Property(user => user.NormalizedEmail).HasMaxLength(254).IsRequired();
            entity.Property(user => user.PasswordHash).IsRequired();
            entity.Property(user => user.Gender).HasMaxLength(20).IsRequired();
            entity.Property(user => user.Role).HasMaxLength(30).IsRequired();
            entity.Property(user => user.IsEmailVerified).HasDefaultValue(false);
            entity.Property(user => user.CreatedAt).IsRequired();

            entity.HasIndex(user => user.NormalizedEmail).IsUnique();
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(token => token.Id);

            entity.Property(token => token.Token).HasMaxLength(256).IsRequired();
            entity.Property(token => token.ExpiresAt).IsRequired();
            entity.Property(token => token.CreatedAt).IsRequired();

            entity.HasIndex(token => token.Token).IsUnique();

            entity.HasOne(token => token.User)
                .WithMany(user => user.RefreshTokens)
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
