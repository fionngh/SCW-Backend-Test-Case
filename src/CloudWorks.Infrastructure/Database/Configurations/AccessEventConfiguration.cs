using Domain.Access;
using Domain.Doors;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Database.Configurations;

internal sealed class AccessEventConfiguration : IEntityTypeConfiguration<AccessEvent>
{
    public void Configure(EntityTypeBuilder<AccessEvent> builder)
    {
        builder.ToTable("access_events", "audit");
        builder.HasKey(accessEvent => accessEvent.Id);
        builder.Property(accessEvent => accessEvent.Id).ValueGeneratedNever();
        builder.Property(accessEvent => accessEvent.OccurredAt);
        builder.Property(accessEvent => accessEvent.DoorName).HasMaxLength(100);
        builder.Property(accessEvent => accessEvent.UserDisplayName).HasMaxLength(100);
        builder
            .Property(accessEvent => accessEvent.Outcome)
            .HasConversion<string>()
            .HasMaxLength(30);
        builder
            .Property(accessEvent => accessEvent.Reason)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder
            .HasIndex(accessEvent => new { accessEvent.DoorId, accessEvent.OccurredAt })
            .IsDescending(false, true);
        builder
            .HasIndex(accessEvent => new { accessEvent.UserId, accessEvent.OccurredAt })
            .IsDescending(false, true);
        builder.HasIndex(accessEvent => accessEvent.OccurredAt);

        builder
            .HasOne<Door>()
            .WithMany()
            .HasForeignKey(accessEvent => accessEvent.DoorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(accessEvent => accessEvent.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
