using Microsoft.EntityFrameworkCore;
using VirtualTryOn.Api.Models;

namespace VirtualTryOn.Api.Data;

public class AppDbContext(
    DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<UserProfile> UserProfiles =>
    Set<UserProfile>();

public DbSet<UserFitProfile> UserFitProfiles =>
    Set<UserFitProfile>();

public DbSet<UserTryOnPhotos> UserTryOnPhotos =>
    Set<UserTryOnPhotos>();

public DbSet<UserDeliveryAddress> UserDeliveryAddresses =>
    Set<UserDeliveryAddress>();


    public DbSet<RefreshToken> RefreshTokens =>
        Set<RefreshToken>();

    public DbSet<PasswordResetToken> PasswordResetTokens =>
        Set<PasswordResetToken>();

    public DbSet<Category> Categories =>
        Set<Category>();

    public DbSet<Product> Products =>
        Set<Product>();

    public DbSet<ProductColor> ProductColors =>
        Set<ProductColor>();

    public DbSet<ProductSize> ProductSizes =>
        Set<ProductSize>();

    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<PromotionProduct> PromotionProducts => Set<PromotionProduct>();
    public DbSet<PromotionEmailDelivery> PromotionEmailDeliveries => Set<PromotionEmailDelivery>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

        public DbSet<CartItem> CartItems =>
    Set<CartItem>();

    public DbSet<ProductReview> ProductReviews =>
    Set<ProductReview>();

public DbSet<Favorite> Favorites =>
    Set<Favorite>();


public DbSet<UserNotification> Notifications =>
    Set<UserNotification>();

    public DbSet<RecentlyViewedProduct>
    RecentlyViewedProducts =>
        Set<RecentlyViewedProduct>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUser(modelBuilder);
        ConfigureUserProfile(modelBuilder);
ConfigureUserFitProfile(modelBuilder);
ConfigureUserTryOnPhotos(modelBuilder);
ConfigureUserDeliveryAddress(modelBuilder);
        ConfigureRefreshToken(modelBuilder);
        ConfigurePasswordResetToken(modelBuilder);
        ConfigureCategory(modelBuilder);
        ConfigureProduct(modelBuilder);
        ConfigureCartItem(modelBuilder);
        ConfigureUserNotification(modelBuilder);
        ConfigureProductReview(modelBuilder);
        ConfigureRecentlyViewedProduct(
    modelBuilder);
        ConfigureFavorite(modelBuilder);
        ConfigureProductColor(modelBuilder);
        ConfigureProductSize(modelBuilder);
        ConfigurePromotion(modelBuilder);
        ConfigureOrder(modelBuilder);
    }

    private static void ConfigureOrder(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.OrderNumber).HasMaxLength(30).IsRequired();
            entity.Property(item => item.Status).HasMaxLength(30).IsRequired();
            entity.Property(item => item.Subtotal).HasPrecision(10, 2);
            entity.Property(item => item.DeliveryFee).HasPrecision(10, 2);
            entity.Property(item => item.Total).HasPrecision(10, 2);
            entity.Property(item => item.TrackingNumber).HasMaxLength(100);
            entity.HasIndex(item => item.OrderNumber).IsUnique();
            entity.HasOne(item => item.User).WithMany(user => user.Orders)
                .HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("OrderItems");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.ProductName).HasMaxLength(160).IsRequired();
            entity.Property(item => item.ImageUrl).HasMaxLength(2048).IsRequired();
            entity.Property(item => item.SizeName).HasMaxLength(30).IsRequired();
            entity.Property(item => item.ColorName).HasMaxLength(60);
            entity.Property(item => item.OriginalUnitPrice).HasPrecision(10, 2);
            entity.Property(item => item.UnitPrice).HasPrecision(10, 2);
            entity.Property(item => item.LineTotal).HasPrecision(10, 2);
            entity.HasOne(item => item.Order).WithMany(order => order.Items)
                .HasForeignKey(item => item.OrderId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Product).WithMany(product => product.OrderItems)
                .HasForeignKey(item => item.ProductId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.ProductSize).WithMany(size => size.OrderItems)
                .HasForeignKey(item => item.ProductSizeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.ProductColor).WithMany(color => color.OrderItems)
                .HasForeignKey(item => item.ProductColorId).OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigurePromotion(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.ToTable("Promotions");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Name).HasMaxLength(160).IsRequired();
            entity.Property(item => item.DiscountPercentage).HasPrecision(5, 2);
            entity.Property(item => item.CreatedAt).IsRequired();
        });

        modelBuilder.Entity<PromotionProduct>(entity =>
        {
            entity.ToTable("PromotionProducts");
            entity.HasKey(item => new { item.PromotionId, item.ProductId });
            entity.HasOne(item => item.Promotion).WithMany(item => item.Products)
                .HasForeignKey(item => item.PromotionId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Product).WithMany(item => item.Promotions)
                .HasForeignKey(item => item.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PromotionEmailDelivery>(entity =>
        {
            entity.ToTable("PromotionEmailDeliveries");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Email).HasMaxLength(254).IsRequired();
            entity.Property(item => item.ErrorMessage).HasMaxLength(1000);
            entity.HasIndex(item => new { item.PromotionId, item.UserId }).IsUnique();
            entity.HasOne(item => item.Promotion).WithMany(item => item.EmailDeliveries)
                .HasForeignKey(item => item.PromotionId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.User).WithMany(item => item.PromotionEmailDeliveries)
                .HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureUser(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");

            entity.HasKey(user => user.Id);

            entity.Property(user => user.FullName)
                .HasMaxLength(120)
                .IsRequired();

            entity.Property(user => user.Email)
                .HasMaxLength(254)
                .IsRequired();

            entity.Property(user => user.NormalizedEmail)
                .HasMaxLength(254)
                .IsRequired();

            entity.Property(user => user.PasswordHash)
                .IsRequired();

            entity.Property(user => user.GoogleSubject)
                .HasMaxLength(128);

            entity.Property(user => user.Gender)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(user => user.ShoppingPreference)
            .HasMaxLength(20);

            entity.Property(user => user.Role)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(user => user.IsEmailVerified)
                .HasDefaultValue(false);

            entity.Property(user => user.CreatedAt)
                .IsRequired();

            entity.HasIndex(user => user.NormalizedEmail)
                .IsUnique();

            entity.HasIndex(user => user.GoogleSubject)
                .IsUnique()
                .HasFilter("[GoogleSubject] IS NOT NULL");
        });
    }

private static void
    ConfigureRecentlyViewedProduct(
        ModelBuilder modelBuilder)
{
    modelBuilder
        .Entity<RecentlyViewedProduct>(
            entity =>
            {
                entity.ToTable(
                    "RecentlyViewedProducts");

                entity.HasKey(view => new
                {
                    view.UserId,
                    view.ProductId
                });

                entity.Property(view =>
                        view.ViewedAt)
                    .IsRequired();

                entity.HasIndex(view =>
                    new
                    {
                        view.UserId,
                        view.ViewedAt
                    });

                entity.HasOne(view =>
                        view.User)
                    .WithMany(user =>
                        user.RecentlyViewedProducts)
                    .HasForeignKey(view =>
                        view.UserId)
                    .OnDelete(
                        DeleteBehavior.Cascade);

                entity.HasOne(view =>
                        view.Product)
                    .WithMany(product =>
                        product.RecentlyViewedByUsers)
                    .HasForeignKey(view =>
                        view.ProductId)
                    .OnDelete(
                        DeleteBehavior.Cascade);
            });
}

private static void ConfigureFavorite(
    ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Favorite>(entity =>
    {
        entity.ToTable("Favorites");

        entity.HasKey(favorite => new
        {
            favorite.UserId,
            favorite.ProductId
        });

        entity.Property(favorite =>
                favorite.CreatedAt)
            .IsRequired();

        entity.HasIndex(favorite =>
            favorite.ProductId);

        entity.HasOne(favorite =>
                favorite.User)
            .WithMany(user =>
                user.Favorites)
            .HasForeignKey(favorite =>
                favorite.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(favorite =>
                favorite.Product)
            .WithMany(product =>
                product.Favorites)
            .HasForeignKey(favorite =>
                favorite.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    });
}
    private static void ConfigureRefreshToken(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");

            entity.HasKey(token => token.Id);

            entity.Property(token => token.Token)
                .HasMaxLength(256)
                .IsRequired();

            entity.Property(token => token.ExpiresAt)
                .IsRequired();

            entity.Property(token => token.CreatedAt)
                .IsRequired();

            entity.HasIndex(token => token.Token)
                .IsUnique();

            entity.HasOne(token => token.User)
                .WithMany(user => user.RefreshTokens)
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigurePasswordResetToken(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.ToTable("PasswordResetTokens");

            entity.HasKey(token => token.Id);

            entity.Property(token => token.TokenHash)
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(token => token.ExpiresAt)
                .IsRequired();

            entity.Property(token => token.CreatedAt)
                .IsRequired();

            entity.HasIndex(token => token.TokenHash)
                .IsUnique();

            entity.HasIndex(token => token.UserId);

            entity.HasOne(token => token.User)
                .WithMany(user => user.PasswordResetTokens)
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureCategory(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");

            entity.HasKey(category => category.Id);

            entity.Property(category => category.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(category => category.Slug)
                .HasMaxLength(120)
                .IsRequired();

            entity.Property(category => category.Audience)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(category => category.ImageUrl)
                .HasMaxLength(2048);

            entity.Property(category => category.DisplayOrder)
                .IsRequired();

            entity.Property(category => category.IsActive)
                .HasDefaultValue(true);

            entity.Property(category => category.CreatedAt)
                .IsRequired();

            entity.HasIndex(category => new
                {
                    category.Audience,
                    category.Slug
                })
                .IsUnique();

            entity.HasIndex(category => new
                {
                    category.Audience,
                    category.DisplayOrder
                });
        });
    }

    private static void ConfigureProduct(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");

            entity.HasKey(product => product.Id);

            entity.Property(product => product.Name)
                .HasMaxLength(160)
                .IsRequired();

            entity.Property(product => product.Slug)
                .HasMaxLength(180)
                .IsRequired();

            entity.Property(product => product.Description)
                .HasMaxLength(2000)
                .IsRequired();

            entity.Property(product => product.Price)
                .HasPrecision(10, 2)
                .IsRequired();

            entity.Property(product => product.ImageUrl)
                .HasMaxLength(2048)
                .IsRequired();

            entity.Property(product => product.Badge)
                .HasMaxLength(30);

            entity.Property(product => product.Rating)
                .HasPrecision(3, 2)
                .HasDefaultValue(0m);

            entity.Property(product => product.ReviewCount)
                .HasDefaultValue(0);

            entity.Property(product => product.IsNew)
                .HasDefaultValue(false);

            entity.Property(product => product.IsActive)
                .HasDefaultValue(true);

            entity.Property(product => product.CreatedAt)
                .IsRequired();

            entity.HasIndex(product => product.CategoryId);

            entity.HasIndex(product => new
                {
                    product.CategoryId,
                    product.Slug
                })
                .IsUnique();

            entity.HasOne(product => product.Category)
                .WithMany(category => category.Products)
                .HasForeignKey(product => product.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureProductColor(
    ModelBuilder modelBuilder)
{
    modelBuilder.Entity<ProductColor>(entity =>
    {
        entity.ToTable("ProductColors");

        entity.HasKey(color => color.Id);

        entity.Property(color => color.Name)
            .HasMaxLength(60)
            .IsRequired();

        entity.Property(color => color.HexCode)
            .HasMaxLength(9)
            .IsRequired();

        entity.Property(color => color.ImageUrl)
            .HasMaxLength(2048);

        entity.HasIndex(color => new
            {
                color.ProductId,
                color.Name
            })
            .IsUnique();

        entity.HasOne(color => color.Product)
            .WithMany(product => product.Colors)
            .HasForeignKey(color => color.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    });
}
    private static void ConfigureProductSize(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductSize>(entity =>
        {
            entity.ToTable("ProductSizes");

            entity.HasKey(size => size.Id);

            entity.Property(size => size.Name)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(size => size.StockQuantity)
                .HasDefaultValue(0);

            entity.HasIndex(size => new
                {
                    size.ProductId,
                    size.Name
                })
                .IsUnique();

            entity.HasOne(size => size.Product)
                .WithMany(product => product.Sizes)
                .HasForeignKey(size => size.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
   private static void ConfigureCartItem(
    ModelBuilder modelBuilder)
{
    modelBuilder.Entity<CartItem>(entity =>
    {
        entity.ToTable("CartItems");

        entity.HasKey(item => item.Id);

        entity.Property(item => item.Quantity)
            .HasDefaultValue(1)
            .IsRequired();

        entity.Property(item => item.CreatedAt)
            .IsRequired();

        entity.HasIndex(item => new
        {
            item.UserId,
            item.ProductSizeId,
            item.ProductColorId
        })
        .IsUnique();

        entity.HasIndex(item => item.UserId);

        entity.HasOne(item => item.User)
            .WithMany(user => user.CartItems)
            .HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(item => item.ProductSize)
            .WithMany(size => size.CartItems)
            .HasForeignKey(item =>
                item.ProductSizeId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(item => item.ProductColor)
            .WithMany(color => color.CartItems)
            .HasForeignKey(item =>
                item.ProductColorId)
            .OnDelete(DeleteBehavior.Restrict);
    });
}

private static void ConfigureProductReview(
    ModelBuilder modelBuilder)
{
    modelBuilder.Entity<ProductReview>(entity =>
    {
        entity.ToTable("ProductReviews");

        entity.HasKey(review => review.Id);

        entity.Property(review => review.Rating)
            .IsRequired();

        entity.Property(review => review.Comment)
            .HasMaxLength(2000)
            .IsRequired();

        entity.Property(review => review.CreatedAt)
            .IsRequired();

        entity.HasIndex(review => new
        {
            review.UserId,
            review.ProductId
        })
        .IsUnique();

        entity.HasIndex(review => new
        {
            review.ProductId,
            review.CreatedAt
        });

        entity.HasOne(review => review.User)
            .WithMany(user => user.Reviews)
            .HasForeignKey(review => review.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(review => review.Product)
            .WithMany(product => product.Reviews)
            .HasForeignKey(review => review.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    });
}
private static void ConfigureUserNotification(
    ModelBuilder modelBuilder)
{
    modelBuilder.Entity<UserNotification>(entity =>
    {
        entity.ToTable("Notifications");

        entity.HasKey(notification =>
            notification.Id);

        entity.Property(notification =>
                notification.Title)
            .HasMaxLength(160)
            .IsRequired();

        entity.Property(notification =>
                notification.Message)
            .HasMaxLength(1000)
            .IsRequired();

        entity.Property(notification =>
                notification.Type)
            .HasMaxLength(40)
            .IsRequired();

        entity.Property(notification =>
                notification.Link)
            .HasMaxLength(500);

        entity.Property(notification =>
                notification.CreatedAt)
            .IsRequired();

        entity.HasIndex(notification => new
        {
            notification.UserId,
            notification.CreatedAt
        });

        entity.HasIndex(notification => new
        {
            notification.UserId,
            notification.IsRead
        });

        entity.HasOne(notification =>
                notification.User)
            .WithMany(user =>
                user.Notifications)
            .HasForeignKey(notification =>
                notification.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    });
}
private static void ConfigureUserProfile(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<UserProfile>(entity =>
    {
        entity.ToTable("UserProfiles");
        entity.HasKey(profile => profile.UserId);

        entity.Property(profile => profile.FullName)
            .HasMaxLength(120).IsRequired();
        entity.Property(profile => profile.Gender)
            .HasMaxLength(20).IsRequired();
        entity.Property(profile => profile.PhoneNumber)
            .HasMaxLength(30);

        entity.HasOne(profile => profile.User)
            .WithOne(user => user.Profile)
            .HasForeignKey<UserProfile>(profile => profile.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    });
}

private static void ConfigureUserFitProfile(ModelBuilder modelBuilder)
{
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
}

private static void ConfigureUserTryOnPhotos(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<UserTryOnPhotos>(entity =>
    {
        entity.ToTable("UserTryOnPhotos");
        entity.HasKey(photos => photos.UserId);

        entity.HasOne(photos => photos.User)
            .WithOne(user => user.TryOnPhotos)
            .HasForeignKey<UserTryOnPhotos>(photos => photos.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    });
}

private static void ConfigureUserDeliveryAddress(ModelBuilder modelBuilder)
{
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
}
}
