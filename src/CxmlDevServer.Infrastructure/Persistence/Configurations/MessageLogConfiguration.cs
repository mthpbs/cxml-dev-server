using CxmlDevServer.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CxmlDevServer.Infrastructure.Persistence.Configurations;

internal sealed class MessageLogConfiguration : IEntityTypeConfiguration<CxmlMessageLog>
{
    public void Configure(EntityTypeBuilder<CxmlMessageLog> builder)
    {
        builder.ToTable("MessageLogs");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.DocumentType).IsRequired().HasMaxLength(100);
        builder.Property(l => l.PayloadId).HasMaxLength(500);
        builder.Property(l => l.InReplyTo).HasMaxLength(500);
        builder.Property(l => l.CorrelationId).HasMaxLength(500);
        builder.Property(l => l.RemoteEndpoint).HasMaxLength(2000);
        builder.Property(l => l.CxmlStatusText).HasMaxLength(200);
        builder.Property(l => l.ContentType).HasMaxLength(200);
        builder.Property(l => l.RawXml).IsRequired();
        builder.Property(l => l.Direction).HasConversion<string>().HasMaxLength(20);
        builder.Property(l => l.Channel).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(l => l.CreatedAt);
        builder.HasIndex(l => l.PayloadId);
        builder.HasIndex(l => l.CorrelationId);
        builder.HasIndex(l => new { l.PartnerId, l.DocumentType });
        builder.HasOne(l => l.Partner).WithMany().HasForeignKey(l => l.PartnerId).OnDelete(DeleteBehavior.SetNull);
        builder.HasMany(l => l.Issues).WithOne(i => i.MessageLog).HasForeignKey(i => i.MessageLogId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ValidationIssueRecordConfiguration : IEntityTypeConfiguration<ValidationIssueRecord>
{
    public void Configure(EntityTypeBuilder<ValidationIssueRecord> builder)
    {
        builder.ToTable("ValidationIssues");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Code).IsRequired().HasMaxLength(100);
        builder.Property(i => i.Message).IsRequired();
        builder.Property(i => i.Severity).HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.Source).HasConversion<string>().HasMaxLength(20);
    }
}
