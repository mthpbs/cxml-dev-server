using CxmlDevServer.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CxmlDevServer.Infrastructure.Persistence.Configurations;

internal sealed class PunchOutSessionConfiguration : IEntityTypeConfiguration<PunchOutSession>
{
    public void Configure(EntityTypeBuilder<PunchOutSession> builder)
    {
        builder.ToTable("PunchOutSessions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Token).IsRequired().HasMaxLength(100);
        builder.Property(s => s.BuyerCookie).IsRequired();
        builder.Property(s => s.SetupPayloadId).IsRequired().HasMaxLength(500);
        builder.Property(s => s.CorrelationId).IsRequired().HasMaxLength(100);
        builder.Property(s => s.DeploymentMode).HasMaxLength(20);
        builder.Property(s => s.BuyerDomain).HasMaxLength(100);
        builder.Property(s => s.BuyerIdentity).HasMaxLength(300);
        builder.Property(s => s.BrowserFormPostUrl).HasMaxLength(2000);
        builder.Property(s => s.SelectedSupplierPartId).HasMaxLength(100);
        builder.Property(s => s.Operation).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(s => s.Token).IsUnique();
        builder.HasIndex(s => s.CorrelationId);
        builder.HasIndex(s => s.SetupPayloadId);
        builder.HasOne(s => s.Partner).WithMany().HasForeignKey(s => s.PartnerId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(s => s.SetupMessageLog).WithMany().HasForeignKey(s => s.SetupMessageLogId).OnDelete(DeleteBehavior.SetNull);
        builder.HasMany(s => s.Lines).WithOne(l => l.Session).HasForeignKey(l => l.SessionId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class PunchOutCartLineConfiguration : IEntityTypeConfiguration<PunchOutCartLine>
{
    public void Configure(EntityTypeBuilder<PunchOutCartLine> builder)
    {
        builder.ToTable("PunchOutCartLines");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.SupplierPartId).IsRequired().HasMaxLength(100);
        builder.Property(l => l.ShortName).IsRequired().HasMaxLength(200);
        builder.Property(l => l.Currency).IsRequired().HasMaxLength(3);
        builder.Property(l => l.UnitOfMeasure).IsRequired().HasMaxLength(10);
        builder.Property(l => l.Unspsc).HasMaxLength(50);
        builder.Property(l => l.ClassificationDomain).HasMaxLength(50);
        builder.HasOne(l => l.CatalogItem).WithMany().HasForeignKey(l => l.CatalogItemId).OnDelete(DeleteBehavior.SetNull);
    }
}
