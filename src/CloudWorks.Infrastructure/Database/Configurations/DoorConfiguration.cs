using Domain.Doors;
using Domain.Doors.Enums;
using Domain.Offices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Database.Configurations;

internal sealed class DoorConfiguration : IEntityTypeConfiguration<Door>
{
    public void Configure(EntityTypeBuilder<Door> builder)
    {
        builder.ToTable("doors", "facility");
        builder.HasKey(door => door.Id);
        builder.Property(door => door.Id).ValueGeneratedNever();
        builder.Property(door => door.AccessPolicy).HasConversion<string>().HasMaxLength(30);
        builder.UseRowVersion();

        builder
            .HasOne<Office>()
            .WithMany()
            .HasForeignKey(door => door.OfficeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
