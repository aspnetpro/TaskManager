using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManager.Auth.Domain.Entities;

namespace TaskManager.Auth.Infrastructure.Persistence.Configurations;

public sealed class WorkspaceMemberConfiguration : IEntityTypeConfiguration<WorkspaceMember>
{
    public void Configure(EntityTypeBuilder<WorkspaceMember> builder)
    {
        builder.ToTable("WorkspaceMembers");
        builder.HasKey(m => new { m.WorkspaceId, m.UserId });
        builder.Property(m => m.Role).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(m => m.UserId);
    }
}
