using GestorFinanceiro.Financeiro.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorFinanceiro.Financeiro.Infra.Config;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts");

        builder.HasKey(account => account.Id);

        builder.Property(account => account.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(account => account.Name)
            .HasColumnName("name")
            .HasColumnType("varchar(150)")
            .IsRequired();

        builder.Property(account => account.Type)
            .HasColumnName("type")
            .HasColumnType("smallint")
            .HasConversion<short>()
            .IsRequired();

        builder.Property(account => account.Balance)
            .HasColumnName("balance")
            .HasColumnType("numeric(18,2)")
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(account => account.AllowNegativeBalance)
            .HasColumnName("allow_negative_balance")
            .HasColumnType("boolean")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(account => account.IsActive)
            .HasColumnName("is_active")
            .HasColumnType("boolean")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(account => account.CreatedBy)
            .HasColumnName("created_by")
            .HasColumnType("varchar(100)")
            .IsRequired();

        builder.Property(account => account.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(account => account.UpdatedBy)
            .HasColumnName("updated_by")
            .HasColumnType("varchar(100)");

        builder.Property(account => account.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");
    }
}
