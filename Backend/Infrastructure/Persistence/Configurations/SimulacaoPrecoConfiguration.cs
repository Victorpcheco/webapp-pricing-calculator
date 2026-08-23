// Infrastructure/Persistence/Configurations/SimulacaoPrecoConfiguration.cs
using Domain.Entities.Precificacoes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class SimulacaoPrecoConfiguration : IEntityTypeConfiguration<SimulacaoPreco>
{
    public void Configure(EntityTypeBuilder<SimulacaoPreco> builder)
    {
        builder.ToTable("simulacoes_preco");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.UsuarioId)
            .HasColumnName("usuario_id")
            .IsRequired();

        builder.Property(x => x.ProdutoId)
            .HasColumnName("produto_id")
            .IsRequired();

        builder.Property(x => x.ProdutoNome)
            .HasColumnName("produto_nome")
            .HasMaxLength(SimulacaoPreco.NomeProdutoTamanhoMaximo)
            .IsRequired();

        builder.Property(x => x.CustoBase)
            .HasColumnName("custo_base")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.Margem)
            .HasColumnName("margem")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.PrecoPraticado)
            .HasColumnName("preco_praticado")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.Quantidade)
            .HasColumnName("quantidade")
            .IsRequired();

        builder.Property(x => x.PrecoSugerido)
            .HasColumnName("preco_sugerido")
            .HasPrecision(18, 4);

        builder.Property(x => x.LucroUnitario)
            .HasColumnName("lucro_unitario")
            .HasPrecision(18, 4);

        builder.Property(x => x.MargemReal)
            .HasColumnName("margem_real")
            .HasPrecision(18, 4);

        builder.Property(x => x.ReceitaEstimada)
            .HasColumnName("receita_estimada")
            .HasPrecision(18, 4);

        builder.Property(x => x.LucroTotalEstimado)
            .HasColumnName("lucro_total_estimado")
            .HasPrecision(18, 4);

        builder.Property(x => x.CriadoEm)
            .HasColumnName("criado_em")
            .IsRequired();

        builder.HasIndex(x => x.UsuarioId)
            .HasDatabaseName("ix_simulacoes_preco_usuario_id");

        // Referência solta ao produto (sem FK) — só acelera a busca, não a integridade
        builder.HasIndex(x => x.ProdutoId)
            .HasDatabaseName("ix_simulacoes_preco_produto_id");
    }
}
