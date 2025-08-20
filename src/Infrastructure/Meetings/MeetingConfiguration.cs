using Domain.Meetings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Meetings;

internal sealed class MeetingConfiguration : IEntityTypeConfiguration<Meeting>
{
    public void Configure(EntityTypeBuilder<Meeting> builder)
    {
        builder.ToTable("meetings");

        builder.HasKey(static m => m.Id);

        builder.Property(static m => m.Id)
            .ValueGeneratedOnAdd();

        builder.Property(static m => m.StartTime)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(static m => m.EndTime)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        // Configure the ParticipantIds as a JSON column for simplicity
        // In a production system, you might want a separate MeetingParticipants table
        builder.Property(static m => m.ParticipantIds)
            .HasConversion(
                static v => string.Join(',', v),
                static v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                      .Select(int.Parse)
                      .ToList()
                      .AsReadOnly())
            .HasColumnName("participant_ids")
            .HasColumnType("text");

        // Add indexes for better query performance
        builder.HasIndex(static m => m.StartTime);
        builder.HasIndex(static m => m.EndTime);

        // Composite index for time range queries
        builder.HasIndex(static m => new { m.StartTime, m.EndTime });
    }
}