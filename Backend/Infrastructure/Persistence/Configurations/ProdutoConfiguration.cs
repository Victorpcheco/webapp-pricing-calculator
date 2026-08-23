// Infrastructure/Persistence/Configurations/ProdutoConfiguration.cs
using Domain.Entities.Produtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ProdutoConfiguration : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> builder)
    {
        builder.ToTable("produtos");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.UsuarioId)
            .HasColumnName("usuario_id")
            .IsRequired();

        builder.Property(x => x.Nome)
            .HasColumnName("nome")
            .HasMaxLength(Produto.NomeTamanhoMaximo)
            .IsRequired();

        builder.Property(x => x.TipoProducao)
            .HasColumnName("tipo_producao")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Rendimento)
            .HasColumnName("rendimento")
            .IsRequired();

        builder.Property(x => x.NomeUnidade)
            .HasColumnName("nome_unidade")
            .HasMaxLength(Produto.NomeUnidadeTamanhoMaximo)
            .IsRequired();

        builder.Property(x => x.TempoProducaoMinutos)
            .HasColumnName("tempo_producao_minutos")
            .IsRequired();

        builder.Property(x => x.CriadoEm)
            .HasColumnName("criado_em")
            .IsRequired();

        builder.Property(x => x.AtualizadoEm)
            .HasColumnName("atualizado_em")
            .IsRequired();

        // Composição é filha do agregado: carregada junto e removida em cascata
        builder.OwnsMany(x => x.Composicao, composicao =>
        {
            composicao.ToTable("produto_composicao");

            composicao.WithOwner().HasForeignKey("produto_id");

            composicao.HasKey(x => x.Id);

            // Chave gerada pelo EF: mantém IsKeySet falso até o insert, para que
            // linhas novas sejam Added e não Modified
            composicao.Property(x => x.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            composicao.Property<Guid>("produto_id")
                .HasColumnName("produto_id");

            composicao.Property(x => x.InsumoId)
                .HasColumnName("insumo_id")
                .IsRequired();

            composicao.Property(x => x.Quantidade)
                .HasColumnName("quantidade")
                .HasPrecision(18, 4)
                .IsRequired();

            composicao.HasIndex("produto_id")
                .HasDatabaseName("ix_produto_composicao_produto_id");

            composicao.HasIndex(x => x.InsumoId)
                .HasDatabaseName("ix_produto_composicao_insumo_id");
        });

        builder.Navigation(x => x.Composicao)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .AutoInclude();

        builder.HasIndex(x => x.UsuarioId)
            .HasDatabaseName("ix_produtos_usuario_id");
    }
}
