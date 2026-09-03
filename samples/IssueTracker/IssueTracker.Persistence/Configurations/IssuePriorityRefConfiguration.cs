using IssueTracker.Application.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IssueTracker.Persistence;

/// <summary>Maps the <c>IssuePriorities</c> lookup table.</summary>
internal sealed class IssuePriorityRefConfiguration : IEntityTypeConfiguration<IssuePriorityRef>
{
    public void Configure(EntityTypeBuilder<IssuePriorityRef> builder)
    {
        builder.ToTable("IssuePriorities");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
        builder.Property(x => x.SortOrder).IsRequired();
    }
}
