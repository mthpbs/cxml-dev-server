using CxmlDevServer.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CxmlDevServer.Infrastructure.Persistence.Configurations;

internal sealed class TradingPartnerConfiguration : IEntityTypeConfiguration<TradingPartner>
{
    public void Configure(EntityTypeBuilder<TradingPartner> builder)
    {
        builder.ToTable("TradingPartners");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.FromDomain).IsRequired().HasMaxLength(100);
        builder.Property(p => p.FromIdentity).IsRequired().HasMaxLength(300);
        builder.Property(p => p.SenderDomain).IsRequired().HasMaxLength(100);
        builder.Property(p => p.SenderIdentity).IsRequired().HasMaxLength(300);
        builder.Property(p => p.ToDomain).HasMaxLength(100);
        builder.Property(p => p.ToIdentity).HasMaxLength(300);
        builder.Property(p => p.SharedSecret).HasMaxLength(300);
        builder.Property(p => p.DeploymentMode).HasMaxLength(20);
        builder.Property(p => p.AuthMode).HasConversion<string>().HasMaxLength(30);
        builder.Property(p => p.ValidationMode).HasConversion<string>().HasMaxLength(30);
        builder.Property(p => p.PunchOutPostEncoding).HasConversion<string>().HasMaxLength(30);
        builder.Property(p => p.OperationAllowed).HasConversion<string>().HasMaxLength(30);
        builder.HasIndex(p => new { p.SenderDomain, p.SenderIdentity });
        builder.HasIndex(p => new { p.FromDomain, p.FromIdentity });
    }
}
