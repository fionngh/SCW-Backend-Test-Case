using Domain.Offices;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Database.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", "identity");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).ValueGeneratedNever();
        builder.HasIndex(user => user.Username).IsUnique().HasDatabaseName("uq_users_username");
        builder.UseRowVersion();

        builder
            .HasOne<Office>()
            .WithMany()
            .HasForeignKey(user => user.OfficeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
