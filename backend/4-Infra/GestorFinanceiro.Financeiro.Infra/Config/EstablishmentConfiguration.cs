using GestorFinanceiro.Financeiro.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorFinanceiro.Financeiro.Infra.Config;

public class EstablishmentConfiguration : IEntityTypeConfiguration<Establishment>
{
    public void Configure(EntityTypeBuilder<Establishment> builder)
    {
        builder.ToTable("establishments");

        builder.HasKey(establishment => establishment.Id);

        builder.Property(establishment => establishment.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(establishment => establishment.TransactionId)
            .HasColumnName("transaction_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(establishment => establishment.Name)
            .HasColumnName("name")
            .HasColumnType("varchar(300)")
            .IsRequired();

        builder.Property(establishment => establishment.Cnpj)
            .HasColumnName("cnpj")
            .HasColumnType("varchar(14)")
            .IsRequired();

        builder.Property(establishment => establishment.AccessKey)
            .HasColumnName("access_key")
            .HasColumnType("varchar(44)")
            .IsRequired();

        builder.Property(establishment => establishment.CreatedBy)
            .HasColumnName("created_by")
            .HasColumnType("varchar(100)")
            .IsRequired();

        builder.Property(establishment => establishment.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(establishment => establishment.UpdatedBy)
            .HasColumnName("updated_by")
            .HasColumnType("varchar(100)");

        builder.Property(establishment => establishment.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        builder.HasOne(establishment => establishment.Transaction)
            .WithMany()
            .HasForeignKey(establishment => establishment.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(establishment => establishment.TransactionId)
            .IsUnique()
            .HasDatabaseName("ix_establishments_transaction_id");

        builder.HasIndex(establishment => establishment.AccessKey)
            .IsUnique()
            .HasDatabaseName("ix_establishments_access_key");
    }
}
