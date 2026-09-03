namespace IssueTracker.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>Maps the persistent <c>Counters</c> table.</summary>
internal sealed class CounterConfiguration : IEntityTypeConfiguration<Counter>
{
    public void Configure(EntityTypeBuilder<Counter> builder)
    {
        builder.ToTable("Counters");
        builder.HasKey(x => x.Name);
        builder.Property(x => x.Name).HasMaxLength(100);
        builder.Property(x => x.Value).IsRequired();
    }
}
