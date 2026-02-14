using GestorFinanceiro.Financeiro.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorFinanceiro.Financeiro.Infra.Config;

public class OperationLogConfiguration : IEntityTypeConfiguration<OperationLog>
{
    public void Configure(EntityTypeBuilder<OperationLog> builder)
    {
        builder.ToTable("operation_logs");

        builder.HasKey(log => log.Id);

        builder.Property(log => log.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(log => log.OperationId)
            .HasColumnName("operation_id")
            .HasColumnType("varchar(100)")
            .IsRequired();

        builder.Property(log => log.OperationType)
            .HasColumnName("operation_type")
            .HasColumnType("varchar(50)")
            .IsRequired();

        builder.Property(log => log.ResultEntityId)
            .HasColumnName("result_entity_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(log => log.ResultPayload)
            .HasColumnName("result_payload")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(log => log.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(log => log.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(log => log.OperationId)
            .HasDatabaseName("ix_operation_logs_operation_id")
            .IsUnique();

        builder.HasIndex(log => log.ExpiresAt)
            .HasDatabaseName("ix_operation_logs_expires_at");
    }
}
