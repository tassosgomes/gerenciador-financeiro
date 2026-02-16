using GestorFinanceiro.Financeiro.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorFinanceiro.Financeiro.Infra.Config;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(auditLog => auditLog.Id);

        builder.Property(auditLog => auditLog.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(auditLog => auditLog.EntityType)
            .HasColumnName("entity_type")
            .HasColumnType("varchar(100)")
            .IsRequired();

        builder.Property(auditLog => auditLog.EntityId)
            .HasColumnName("entity_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(auditLog => auditLog.Action)
            .HasColumnName("action")
            .HasColumnType("varchar(50)")
            .IsRequired();

        builder.Property(auditLog => auditLog.UserId)
            .HasColumnName("user_id")
            .HasColumnType("varchar(100)")
            .IsRequired();

        builder.Property(auditLog => auditLog.Timestamp)
            .HasColumnName("timestamp")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(auditLog => auditLog.PreviousData)
            .HasColumnName("previous_data")
            .HasColumnType("jsonb");

        builder.HasIndex(auditLog => new { auditLog.EntityType, auditLog.EntityId })
            .HasDatabaseName("ix_audit_logs_entity");

        builder.HasIndex(auditLog => auditLog.UserId)
            .HasDatabaseName("ix_audit_logs_user_id");

        builder.HasIndex(auditLog => auditLog.Timestamp)
            .HasDatabaseName("ix_audit_logs_timestamp");
    }
}
