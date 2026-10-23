using Domain.Offices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Database.Configurations;

internal sealed class OfficeConfiguration : IEntityTypeConfiguration<Office>
{
    public void Configure(EntityTypeBuilder<Office> builder)
    {
        builder.ToTable("offices", "facility");
        builder.HasKey(office => office.Id);
        builder.Property(office => office.Id).ValueGeneratedNever();
        builder.UseRowVersion();
    }
}
