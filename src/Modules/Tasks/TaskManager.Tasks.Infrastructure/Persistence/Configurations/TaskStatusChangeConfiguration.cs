namespace TaskManager.Tasks.Infrastructure.Persistence.Configurations;

public sealed class TaskStatusChangeConfiguration : IEntityTypeConfiguration<TaskStatusChange>
{
    public void Configure(EntityTypeBuilder<TaskStatusChange> builder)
    {
        builder.ToTable("TaskStatusHistory");
        builder.Property<long>("Id");
        builder.HasKey("Id");

        builder.Property(h => h.From).HasConversion<string>().HasMaxLength(20);
        builder.Property(h => h.To).HasConversion<string>().HasMaxLength(20);
    }
}
