using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ServiceName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.EventName).IsRequired().HasMaxLength(150);
        
        // Dados estruturados jsonb
        builder.Property(x => x.EventData).HasColumnType("jsonb").IsRequired();
        
        builder.Property(x => x.CreatedAt).IsRequired();
    }
}
