using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManager.Auth.Domain.Entities;

namespace TaskManager.Auth.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();
        builder.Property(u => u.Email).HasMaxLength(User.MaxEmailLength).IsRequired();
        builder.Property(u => u.FullName).HasMaxLength(User.MaxFullNameLength).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(500).IsRequired();
        builder.Ignore(u => u.DomainEvents);

        // Email já chega normalizado (minúsculo); o índice único fecha a corrida do registro.
        builder.HasIndex(u => u.Email).IsUnique();
    }
}
