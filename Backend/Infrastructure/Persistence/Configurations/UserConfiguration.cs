using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Nome)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.HasIndex(x => x.Email)
            .IsUnique();
            
        builder.Property(x => x.Telefone)
            .HasMaxLength(20);
            
        builder.Property(x => x.SenhaHash)
            .IsRequired();
    }
}
