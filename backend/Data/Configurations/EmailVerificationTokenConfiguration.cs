using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VirtualTryOn.Api.Models;

namespace VirtualTryOn.Api.Data.Configurations;

public class EmailVerificationTokenConfiguration :
    IEntityTypeConfiguration<EmailVerificationToken>
{
    public void Configure(
        EntityTypeBuilder<EmailVerificationToken> entity)
    {
        entity.ToTable("EmailVerificationTokens");

        entity.HasKey(token => token.Id);

        entity.Property(token => token.TokenHash)
            .HasMaxLength(128)
            .IsRequired();

        entity.HasIndex(token => token.TokenHash)
            .IsUnique();

        entity.HasIndex(token => new
        {
            token.UserId,
            token.ExpiresAt
        });

        entity.HasOne(token => token.User)
            .WithMany(user => user.EmailVerificationTokens)
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
