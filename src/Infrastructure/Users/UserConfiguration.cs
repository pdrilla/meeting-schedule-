using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Users;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("meeting_users");

        builder.HasKey(static u => u.Id);

        builder.Property(static u => u.Id)
            .ValueGeneratedOnAdd();

        builder.Property(static u => u.Name)
            .IsRequired()
            .HasMaxLength(100);
    }
}
