using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Database.Configurations;

internal static class ConcurrencyTokenExtensions
{
    /// <summary>
    /// Uses PostgreSQL's built-in xmin system column as the row version, so concurrent updates to
    /// the same row are detected without adding a column to the domain entity.
    /// </summary>
    public static EntityTypeBuilder<TEntity> UseRowVersion<TEntity>(
        this EntityTypeBuilder<TEntity> builder
    )
        where TEntity : class
    {
        builder
            .Property<uint>("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        return builder;
    }
}
