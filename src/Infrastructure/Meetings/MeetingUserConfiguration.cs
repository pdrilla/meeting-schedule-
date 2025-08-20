using Domain.Meetings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Meetings;

internal sealed class MeetingUserConfiguration : IEntityTypeConfiguration<MeetingUser>
{
    public void Configure(EntityTypeBuilder<MeetingUser> builder)
    {
        builder.ToTable("meeting_users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .ValueGeneratedOnAdd();

        builder.Property(u => u.Name)
            .IsRequired()
            .HasMaxLength(100);
    }
}