using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManager.Auth.Domain.Entities;

namespace TaskManager.Auth.Infrastructure.Persistence.Configurations;

public sealed class WorkspaceConfiguration : IEntityTypeConfiguration<Workspace>
{
    public void Configure(EntityTypeBuilder<Workspace> builder)
    {
        builder.ToTable("Workspaces");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).ValueGeneratedNever();
        builder.Property(w => w.Name).HasMaxLength(Workspace.MaxNameLength).IsRequired();
        builder.Ignore(w => w.DomainEvents);

        builder.HasMany(w => w.Members)
            .WithOne()
            .HasForeignKey(m => m.WorkspaceId);

        builder.Navigation(w => w.Members).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
