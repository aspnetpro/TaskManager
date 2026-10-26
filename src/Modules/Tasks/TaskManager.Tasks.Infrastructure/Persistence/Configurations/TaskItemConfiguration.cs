namespace TaskManager.Tasks.Infrastructure.Persistence.Configurations;

public sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("Tasks");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        // Complex types (não HasConversion): o EF consegue traduzir
        // t.Title.Value.Contains(...) para SQL. Com value converter, a busca
        // textual da US-002 não traduz.
        builder.ComplexProperty(t => t.Title, title =>
            title.Property(v => v.Value)
                .HasColumnName("Title")
                .HasMaxLength(Title.MaxLength)
                .IsRequired());

        builder.ComplexProperty(t => t.Description, description =>
            description.Property(v => v.Value)
                .HasColumnName("Description")
                .HasMaxLength(Description.MaxLength));

        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.Priority); // int: ordenação por prioridade correta

        builder.Property(t => t.Version).IsConcurrencyToken();

        builder.Ignore(t => t.DomainEvents);

        builder.HasMany(t => t.StatusHistory)
            .WithOne()
            .HasForeignKey("TaskId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(t => t.StatusHistory).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(t => new { t.WorkspaceId, t.Status });
        builder.HasIndex(t => new { t.WorkspaceId, t.CreatedAt });
        builder.HasIndex(t => new { t.WorkspaceId, t.AssigneeId });
        builder.HasIndex(t => new { t.WorkspaceId, t.DueDate });
    }
}
