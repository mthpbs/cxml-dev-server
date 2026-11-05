using CxmlDevServer.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CxmlDevServer.Infrastructure.Persistence.Configurations;

internal sealed class CatalogItemConfiguration : IEntityTypeConfiguration<CatalogItem>
{
    public void Configure(EntityTypeBuilder<CatalogItem> builder)
    {
        builder.ToTable("CatalogItems");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.SupplierPartId).IsRequired().HasMaxLength(100);
        builder.Property(i => i.SupplierPartAuxiliaryId).HasMaxLength(500);
        builder.Property(i => i.ShortName).IsRequired().HasMaxLength(200);
        builder.Property(i => i.Description).IsRequired().HasMaxLength(2000);
        builder.Property(i => i.Currency).IsRequired().HasMaxLength(3);
        builder.Property(i => i.UnitOfMeasure).IsRequired().HasMaxLength(10);
        builder.Property(i => i.Unspsc).HasMaxLength(20);
        builder.Property(i => i.ManufacturerName).HasMaxLength(200);
        builder.Property(i => i.ManufacturerPartId).HasMaxLength(100);
        builder.Property(i => i.ImageUrl).HasMaxLength(1000);
        builder.Property(i => i.Category).IsRequired().HasMaxLength(100);
        builder.HasIndex(i => i.SupplierPartId).IsUnique();
        builder.HasIndex(i => i.Category);
    }
}
