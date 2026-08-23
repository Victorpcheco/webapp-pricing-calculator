// Infrastructure/Persistence/Configurations/InsumoConfiguration.cs
using Domain.Entities.Insumos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class InsumoConfiguration : IEntityTypeConfiguration<Insumo>
{
    public void Configure(EntityTypeBuilder<Insumo> builder)
    {
        builder.ToTable("insumos");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.UsuarioId)
            .HasColumnName("usuario_id")
            .IsRequired();

        builder.Property(x => x.Nome)
            .HasColumnName("nome")
            .HasMaxLength(Insumo.NomeTamanhoMaximo)
            .IsRequired();

        builder.Property(x => x.Tipo)
            .HasColumnName("tipo")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Quantidade)
            .HasColumnName("quantidade")
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(x => x.Unidade)
            .HasColumnName("unidade")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Preco)
            .HasColumnName("preco")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.QuantidadeBase)
            .HasColumnName("quantidade_base")
            .HasPrecision(18, 3);

        builder.Property(x => x.UnidadeBase)
            .HasColumnName("unidade_base")
            .HasConversion<int>();

        // 6 casas: insumos baratos por grama (ex.: R$ 0,00498/g) zerariam com 2
        builder.Property(x => x.PrecoUnitario)
            .HasColumnName("preco_unitario")
            .HasPrecision(18, 6);

        builder.Property(x => x.CriadoEm)
            .HasColumnName("criado_em")
            .IsRequired();

        builder.Property(x => x.AtualizadoEm)
            .HasColumnName("atualizado_em")
            .IsRequired();

        builder.HasIndex(x => x.UsuarioId)
            .HasDatabaseName("ix_insumos_usuario_id");

        // Cobre o filtro por tipo da toolbar sem varrer todos os insumos do usuário
        builder.HasIndex(x => new { x.UsuarioId, x.Tipo })
            .HasDatabaseName("ix_insumos_usuario_tipo");
    }
}
