using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManager.Notifications.Domain;

namespace TaskManager.Notifications.Infrastructure.Persistence;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();
        builder.Property(n => n.Type).HasConversion<string>().HasMaxLength(30);
        builder.Property(n => n.TaskTitle).HasMaxLength(Notification.MaxTaskTitleLength).IsRequired();
        builder.Property(n => n.Detail).HasMaxLength(Notification.MaxDetailLength);
        builder.Ignore(n => n.DomainEvents);

        // Idempotência do consumidor: o mesmo evento não gera duas notificações para a mesma pessoa.
        builder.HasIndex(n => new { n.SourceEventId, n.RecipientId }).IsUnique();
        builder.HasIndex(n => new { n.WorkspaceId, n.RecipientId, n.CreatedAt });
    }
}
