// Infrastructure/Persistence/Configurations/ColaboradorConfiguration.cs
using Domain.Entities.Colaboradores;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ColaboradorConfiguration : IEntityTypeConfiguration<Colaborador>
{
    public void Configure(EntityTypeBuilder<Colaborador> builder)
    {
        builder.ToTable("colaboradores");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.UsuarioId)
            .HasColumnName("usuario_id")
            .IsRequired();

        builder.Property(x => x.Codigo)
            .HasColumnName("codigo")
            .HasMaxLength(Colaborador.CodigoTamanhoMaximo);

        builder.Property(x => x.Nome)
            .HasColumnName("nome")
            .HasMaxLength(Colaborador.NomeTamanhoMaximo)
            .IsRequired();

        builder.Property(x => x.Cargo)
            .HasColumnName("cargo")
            .HasMaxLength(Colaborador.CargoTamanhoMaximo)
            .IsRequired();

        builder.Property(x => x.TipoContratacao)
            .HasColumnName("tipo_contratacao")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.DataAdmissao)
            .HasColumnName("data_admissao")
            .IsRequired();

        builder.Property(x => x.ValorBase)
            .HasColumnName("valor_base")
            .HasPrecision(18, 2)
            .IsRequired();

        // Nulo para CLT — o vínculo é sempre mensal
        builder.Property(x => x.FrequenciaPagamento)
            .HasColumnName("frequencia_pagamento")
            .HasConversion<int?>();

        builder.Property(x => x.Telefone)
            .HasColumnName("telefone")
            .HasMaxLength(Colaborador.TelefoneTamanhoMaximo);

        // 4 casas: os avos de 13º e férias não fecham em centavos exatos
        builder.Property(x => x.CustoMensal)
            .HasColumnName("custo_mensal")
            .HasPrecision(18, 4);

        builder.Property(x => x.CriadoEm)
            .HasColumnName("criado_em")
            .IsRequired();

        builder.Property(x => x.AtualizadoEm)
            .HasColumnName("atualizado_em")
            .IsRequired();

        // Provisao é derivada de ValorBase — nunca vai para o banco
        builder.Ignore(x => x.Provisao);

        builder.HasIndex(x => x.UsuarioId)
            .HasDatabaseName("ix_colaboradores_usuario_id");

        // Cobre o filtro de contratação da toolbar sem varrer toda a equipe do usuário
        builder.HasIndex(x => new { x.UsuarioId, x.TipoContratacao })
            .HasDatabaseName("ix_colaboradores_usuario_tipo_contratacao");
    }
}
