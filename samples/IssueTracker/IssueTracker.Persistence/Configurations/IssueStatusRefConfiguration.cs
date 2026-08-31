using IssueTracker.Application.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IssueTracker.Persistence;

/// <summary>Maps the <c>IssueStatuses</c> lookup table.</summary>
internal sealed class IssueStatusRefConfiguration : IEntityTypeConfiguration<IssueStatusRef>
{
    public void Configure(EntityTypeBuilder<IssueStatusRef> builder)
    {
        builder.ToTable("IssueStatuses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
        builder.Property(x => x.SortOrder).IsRequired();
    }
}
