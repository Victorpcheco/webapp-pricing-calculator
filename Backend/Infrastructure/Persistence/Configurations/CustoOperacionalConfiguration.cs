// Infrastructure/Persistence/Configurations/CustoOperacionalConfiguration.cs
using Domain.Entities.Custos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class CustoOperacionalConfiguration : IEntityTypeConfiguration<CustoOperacional>
{
    public void Configure(EntityTypeBuilder<CustoOperacional> builder)
    {
        builder.ToTable("custos_operacionais");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.UsuarioId)
            .HasColumnName("usuario_id")
            .IsRequired();

        builder.Property(x => x.Descricao)
            .HasColumnName("descricao")
            .HasMaxLength(200);

        builder.Property(x => x.CriadoEm)
            .HasColumnName("criado_em")
            .IsRequired();

        builder.Property(x => x.ProLabore)
            .HasColumnName("pro_labore")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.HorasMensais)
            .HasColumnName("horas_mensais")
            .IsRequired();

        builder.Property(x => x.ContaEnergia)
            .HasColumnName("conta_energia")
            .HasPrecision(18, 2);

        builder.Property(x => x.PercentualEnergiaTrabalho)
            .HasColumnName("percentual_energia_trabalho")
            .HasPrecision(5, 2);

        builder.Property(x => x.GastoGas)
            .HasColumnName("gasto_gas")
            .HasPrecision(18, 2);

        builder.Property(x => x.PercentualGasTrabalho)
            .HasColumnName("percentual_gas_trabalho")
            .HasPrecision(5, 2);

        builder.Property(x => x.PossuiMei)
            .HasColumnName("possui_mei");

        builder.Property(x => x.ValorDas)
            .HasColumnName("valor_das")
            .HasPrecision(18, 2);

        builder.Property(x => x.TaxaDepreciacao)
            .HasColumnName("taxa_depreciacao")
            .HasPrecision(5, 2);

        builder.Property(x => x.EnergiaReal)
            .HasColumnName("energia_real")
            .HasPrecision(18, 2);

        builder.Property(x => x.GasReal)
            .HasColumnName("gas_real")
            .HasPrecision(18, 2);

        builder.Property(x => x.ValorDepreciacao)
            .HasColumnName("valor_depreciacao")
            .HasPrecision(18, 2);

        builder.Property(x => x.CustoMensal)
            .HasColumnName("custo_mensal")
            .HasPrecision(18, 2);

        builder.Property(x => x.ValorHora)
            .HasColumnName("valor_hora")
            .HasPrecision(18, 4);

        builder.HasIndex(x => x.UsuarioId)
            .HasDatabaseName("ix_custos_operacionais_usuario_id");

        builder.HasIndex(x => new { x.UsuarioId, x.CriadoEm })
            .HasDatabaseName("ix_custos_operacionais_usuario_criado_em")
            .IsDescending(false, true);
    }
}
