using GestorFinanceiro.Financeiro.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorFinanceiro.Financeiro.Infra.Config;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(user => user.Name)
            .HasColumnName("name")
            .HasColumnType("varchar(150)")
            .IsRequired();

        builder.Property(user => user.Email)
            .HasColumnName("email")
            .HasColumnType("varchar(255)")
            .IsRequired();

        builder.Property(user => user.PasswordHash)
            .HasColumnName("password_hash")
            .HasColumnType("varchar(500)")
            .IsRequired();

        builder.Property(user => user.Role)
            .HasColumnName("role")
            .HasColumnType("smallint")
            .HasConversion<short>()
            .IsRequired();

        builder.Property(user => user.IsActive)
            .HasColumnName("is_active")
            .HasColumnType("boolean")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(user => user.MustChangePassword)
            .HasColumnName("must_change_password")
            .HasColumnType("boolean")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(user => user.CreatedBy)
            .HasColumnName("created_by")
            .HasColumnType("varchar(100)")
            .IsRequired();

        builder.Property(user => user.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(user => user.UpdatedBy)
            .HasColumnName("updated_by")
            .HasColumnType("varchar(100)");

        builder.Property(user => user.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(user => user.Email)
            .HasDatabaseName("ix_users_email")
            .IsUnique();
    }
}
