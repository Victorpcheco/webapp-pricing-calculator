using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.NomeServico).IsRequired().HasMaxLength(100);
        builder.Property(x => x.NomeEvento).IsRequired().HasMaxLength(150);
        builder.Property(x => x.DadosEvento).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.DataCriacao).IsRequired();
    }
}
