using IssueTracker.Application.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IssueTracker.Persistence;

/// <summary>
/// Maps the <c>Issues</c> table: unique friendly <c>Key</c>, FK relationships to the status/priority
/// lookup tables and the users table (<c>AssigneeId</c>, <c>ON DELETE SET NULL</c>), plus supporting
/// indexes. The <see cref="IssueStatus"/>/<see cref="IssuePriority"/> enums map to the FK columns.
/// </summary>
internal sealed class IssueConfiguration : IEntityTypeConfiguration<Issue>
{
    public void Configure(EntityTypeBuilder<Issue> builder)
    {
        builder.ToTable("Issues");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Key).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.Key).IsUnique();

        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(4000);

        // Enum values map directly to the lookup FK columns (StatusId / PriorityId).
        builder.Property(x => x.Status).HasColumnName("StatusId").IsRequired();
        builder.Property(x => x.Priority).HasColumnName("PriorityId").IsRequired();

        builder.Property(x => x.CreatedUtc).IsRequired();
        builder.Property(x => x.UpdatedUtc).IsRequired();

        builder.HasOne<IssueStatusRef>()
            .WithMany()
            .HasForeignKey(x => x.Status)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<IssuePriorityRef>()
            .WithMany()
            .HasForeignKey(x => x.Priority)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.AssigneeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.Priority);
        builder.HasIndex(x => x.AssigneeId);
        builder.HasIndex(x => x.CreatedUtc);
    }
}
