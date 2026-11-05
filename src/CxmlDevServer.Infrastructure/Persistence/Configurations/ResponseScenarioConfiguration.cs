using CxmlDevServer.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CxmlDevServer.Infrastructure.Persistence.Configurations;

internal sealed class ResponseScenarioConfiguration : IEntityTypeConfiguration<ResponseScenario>
{
    public void Configure(EntityTypeBuilder<ResponseScenario> builder)
    {
        builder.ToTable("ResponseScenarios");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
        builder.Property(s => s.DocumentType).HasMaxLength(100);
        builder.Property(s => s.StatusText).HasMaxLength(200);
        builder.Property(s => s.Behavior).HasConversion<string>().HasMaxLength(30);
        builder.HasOne(s => s.Partner).WithMany().HasForeignKey(s => s.PartnerId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(s => new { s.Enabled, s.Priority });
    }
}
