using Microsoft.EntityFrameworkCore;

namespace TaskManager.Shared.Infrastructure.Outbox;

public static class OutboxModelBuilderExtensions
{
    public const string TableName = "OutboxMessages";

    /// <summary>Mapeia a tabela de Outbox no schema padrão do DbContext do módulo.</summary>
    public static ModelBuilder AddOutbox(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.ToTable(TableName);
            builder.HasKey(m => m.Id);
            builder.Property(m => m.Id).ValueGeneratedNever();
            builder.Property(m => m.Type).HasMaxLength(OutboxMessage.MaxTypeLength).IsRequired();
            builder.Property(m => m.Content).IsRequired();
            builder.Property(m => m.Error).HasMaxLength(OutboxMessage.MaxErrorLength);

            // O processor só lê pendentes, em ordem de ocorrência.
            builder.HasIndex(m => m.OccurredAt).HasFilter("[ProcessedAt] IS NULL");
        });

        return modelBuilder;
    }
}
