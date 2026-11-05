using CxmlDevServer.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CxmlDevServer.Infrastructure.Persistence.Configurations;

internal sealed class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("PurchaseOrders");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.OrderId).IsRequired().HasMaxLength(200);
        builder.Property(p => p.PayloadId).IsRequired().HasMaxLength(500);
        builder.Property(p => p.Currency).HasMaxLength(3);
        builder.Property(p => p.DeploymentMode).HasMaxLength(20);
        builder.Property(p => p.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(p => new { p.OrderId, p.PartnerId }).IsUnique();
        builder.HasIndex(p => p.PayloadId);
        builder.HasOne(p => p.Partner).WithMany().HasForeignKey(p => p.PartnerId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(p => p.MessageLog).WithMany().HasForeignKey(p => p.MessageLogId).OnDelete(DeleteBehavior.SetNull);
        builder.HasMany(p => p.Lines).WithOne(l => l.PurchaseOrder).HasForeignKey(l => l.PurchaseOrderId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class PurchaseOrderLineConfiguration : IEntityTypeConfiguration<PurchaseOrderLine>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderLine> builder)
    {
        builder.ToTable("PurchaseOrderLines");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.SupplierPartId).IsRequired().HasMaxLength(200);
        builder.Property(l => l.Currency).HasMaxLength(3);
        builder.Property(l => l.UnitOfMeasure).HasMaxLength(20);
        builder.Property(l => l.ClassificationDomain).HasMaxLength(50);
        builder.Property(l => l.Classification).HasMaxLength(100);
        builder.Property(l => l.RequestedDeliveryDate).HasMaxLength(50);
    }
}
