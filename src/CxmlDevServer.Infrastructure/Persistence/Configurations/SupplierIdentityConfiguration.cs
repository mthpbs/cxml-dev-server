using CxmlDevServer.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CxmlDevServer.Infrastructure.Persistence.Configurations;

internal sealed class SupplierIdentityConfiguration : IEntityTypeConfiguration<SupplierIdentity>
{
    public void Configure(EntityTypeBuilder<SupplierIdentity> builder)
    {
        builder.ToTable("SupplierIdentities");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Domain).IsRequired().HasMaxLength(100);
        builder.Property(i => i.Identity).IsRequired().HasMaxLength(300);
        builder.Property(i => i.Name).HasMaxLength(200);
        builder.HasIndex(i => new { i.Domain, i.Identity }).IsUnique();
    }
}
