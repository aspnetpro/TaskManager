using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManager.Auth.Domain.Entities;

namespace TaskManager.Auth.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.TokenHash)
            .HasMaxLength(RefreshToken.TokenHashLength)
            .IsFixedLength()
            .IsUnicode(false)
            .IsRequired();
        builder.Ignore(t => t.DomainEvents);

        // UPDATE ... WHERE RevokedAt IS NULL: dois refreshes com o mesmo token, só um rotaciona.
        builder.Property(t => t.RevokedAt).IsConcurrencyToken();

        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasIndex(t => t.FamilyId);
        builder.HasIndex(t => t.UserId);
    }
}
