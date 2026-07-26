using Microsoft.EntityFrameworkCore;
using VirtualTryOn.Api.Models;

namespace VirtualTryOn.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    public DbSet<UserFitProfile> UserFitProfiles => Set<UserFitProfile>();

    public DbSet<UserTryOnPhotos> UserTryOnPhotos => Set<UserTryOnPhotos>();

    public DbSet<UserDeliveryAddress> UserDeliveryAddresses => Set<UserDeliveryAddress>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(user => user.Id);

            entity.Property(user => user.Email).HasMaxLength(254).IsRequired();
            entity.Property(user => user.NormalizedEmail).HasMaxLength(254).IsRequired();
            entity.Property(user => user.PasswordHash).IsRequired();
            entity.Property(user => user.GoogleSubject).HasMaxLength(128);
            entity.Property(user => user.Role).HasMaxLength(30).IsRequired();
            entity.Property(user => user.IsEmailVerified).HasDefaultValue(false);
            entity.Property(user => user.CreatedAt).IsRequired();

            entity.HasIndex(user => user.NormalizedEmail).IsUnique();
            entity.HasIndex(user => user.GoogleSubject)
                .IsUnique()
                .HasFilter("[GoogleSubject] IS NOT NULL");
        });

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.ToTable("UserProfiles");
            entity.HasKey(profile => profile.UserId);

            entity.Property(profile => profile.FullName).HasMaxLength(120).IsRequired();
            entity.Property(profile => profile.Gender).HasMaxLength(20).IsRequired();
            entity.Property(profile => profile.PhoneNumber).HasMaxLength(30);

            entity.HasOne(profile => profile.User)
                .WithOne(user => user.Profile)
                .HasForeignKey<UserProfile>(profile => profile.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserFitProfile>(entity =>
        {
            entity.ToTable("UserFitProfiles");
            entity.HasKey(profile => profile.UserId);

            entity.Property(profile => profile.HeightCm).HasPrecision(5, 2);
            entity.Property(profile => profile.WeightKg).HasPrecision(5, 2);
            entity.Property(profile => profile.PreferredSize).HasMaxLength(20);
            entity.Property(profile => profile.BodyShape).HasMaxLength(40);
            entity.Property(profile => profile.ShoeSize).HasMaxLength(20);
            entity.Property(profile => profile.TopSize).HasMaxLength(20);
            entity.Property(profile => profile.BottomSize).HasMaxLength(20);

            entity.HasOne(profile => profile.User)
                .WithOne(user => user.FitProfile)
                .HasForeignKey<UserFitProfile>(profile => profile.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserTryOnPhotos>(entity =>
        {
            entity.ToTable("UserTryOnPhotos");
            entity.HasKey(photos => photos.UserId);

            entity.HasOne(photos => photos.User)
                .WithOne(user => user.TryOnPhotos)
                .HasForeignKey<UserTryOnPhotos>(photos => photos.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserDeliveryAddress>(entity =>
        {
            entity.ToTable("UserDeliveryAddresses");
            entity.HasKey(address => address.UserId);

            entity.Property(address => address.Country).HasMaxLength(80);
            entity.Property(address => address.City).HasMaxLength(100);
            entity.Property(address => address.Street).HasMaxLength(160);
            entity.Property(address => address.Building).HasMaxLength(80);
            entity.Property(address => address.PhoneNumber).HasMaxLength(30);

            entity.HasOne(address => address.User)
                .WithOne(user => user.DeliveryAddress)
                .HasForeignKey<UserDeliveryAddress>(address => address.UserId)
                .OnDelete(DeleteBehavior.Cascade);
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

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.ToTable("PasswordResetTokens");
            entity.HasKey(token => token.Id);

            entity.Property(token => token.TokenHash).HasMaxLength(128).IsRequired();
            entity.Property(token => token.ExpiresAt).IsRequired();
            entity.Property(token => token.CreatedAt).IsRequired();

            entity.HasIndex(token => token.TokenHash).IsUnique();
            entity.HasIndex(token => token.UserId);

            entity.HasOne(token => token.User)
                .WithMany(user => user.PasswordResetTokens)
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
