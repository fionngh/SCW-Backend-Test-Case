using Domain.Doors;
using Domain.Permissions;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Database.Configurations;

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions", "identity");
        builder.HasKey(permission => permission.Id);
        builder.Property(permission => permission.Id).ValueGeneratedNever();
        builder.Property(permission => permission.Type).HasConversion<string>().HasMaxLength(30);
        builder.Property(permission => permission.GrantedAt);
        builder.UseRowVersion();
        builder
            .HasIndex(permission => new
            {
                permission.UserId,
                permission.Type,
                permission.DoorId,
            })
            .IsUnique()
            .AreNullsDistinct(false)
            .HasFilter("revoked_at IS NULL")
            .HasDatabaseName("uq_permissions_active_user_type_door");

        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(permission => permission.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne<Door>()
            .WithMany()
            .HasForeignKey(permission => permission.DoorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(permission => permission.GrantedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(permission => permission.RevokedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
