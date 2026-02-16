using GestorFinanceiro.Financeiro.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorFinanceiro.Financeiro.Infra.Config;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable(
            "transactions",
            tableBuilder => tableBuilder.HasCheckConstraint("ck_transactions_amount_positive", "amount > 0"));

        builder.HasKey(transaction => transaction.Id);

        builder.Ignore(transaction => transaction.IsOverdue);

        builder.Property(transaction => transaction.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(transaction => transaction.AccountId)
            .HasColumnName("account_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(transaction => transaction.CategoryId)
            .HasColumnName("category_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(transaction => transaction.Type)
            .HasColumnName("type")
            .HasColumnType("smallint")
            .HasConversion<short>()
            .IsRequired();

        builder.Property(transaction => transaction.Amount)
            .HasColumnName("amount")
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(transaction => transaction.Description)
            .HasColumnName("description")
            .HasColumnType("varchar(500)")
            .IsRequired();

        builder.Property(transaction => transaction.CompetenceDate)
            .HasColumnName("competence_date")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(transaction => transaction.DueDate)
            .HasColumnName("due_date")
            .HasColumnType("date");

        builder.Property(transaction => transaction.Status)
            .HasColumnName("status")
            .HasColumnType("smallint")
            .HasConversion<short>()
            .IsRequired();

        builder.Property(transaction => transaction.IsAdjustment)
            .HasColumnName("is_adjustment")
            .HasColumnType("boolean")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(transaction => transaction.OriginalTransactionId)
            .HasColumnName("original_transaction_id")
            .HasColumnType("uuid");

        builder.Property(transaction => transaction.HasAdjustment)
            .HasColumnName("has_adjustment")
            .HasColumnType("boolean")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(transaction => transaction.InstallmentGroupId)
            .HasColumnName("installment_group_id")
            .HasColumnType("uuid");

        builder.Property(transaction => transaction.InstallmentNumber)
            .HasColumnName("installment_number")
            .HasColumnType("smallint")
            .HasConversion(value => (short?)value, value => (int?)value);

        builder.Property(transaction => transaction.TotalInstallments)
            .HasColumnName("total_installments")
            .HasColumnType("smallint")
            .HasConversion(value => (short?)value, value => (int?)value);

        builder.Property(transaction => transaction.IsRecurrent)
            .HasColumnName("is_recurrent")
            .HasColumnType("boolean")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(transaction => transaction.RecurrenceTemplateId)
            .HasColumnName("recurrence_template_id")
            .HasColumnType("uuid");

        builder.Property(transaction => transaction.TransferGroupId)
            .HasColumnName("transfer_group_id")
            .HasColumnType("uuid");

        builder.Property(transaction => transaction.CancellationReason)
            .HasColumnName("cancellation_reason")
            .HasColumnType("varchar(500)");

        builder.Property(transaction => transaction.CancelledBy)
            .HasColumnName("cancelled_by")
            .HasColumnType("varchar(100)");

        builder.Property(transaction => transaction.CancelledAt)
            .HasColumnName("cancelled_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(transaction => transaction.OperationId)
            .HasColumnName("operation_id")
            .HasColumnType("varchar(100)");

        builder.Property(transaction => transaction.CreatedBy)
            .HasColumnName("created_by")
            .HasColumnType("varchar(100)")
            .IsRequired();

        builder.Property(transaction => transaction.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(transaction => transaction.UpdatedBy)
            .HasColumnName("updated_by")
            .HasColumnType("varchar(100)");

        builder.Property(transaction => transaction.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        builder.HasOne(transaction => transaction.Account)
            .WithMany()
            .HasForeignKey(transaction => transaction.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(transaction => transaction.Category)
            .WithMany()
            .HasForeignKey(transaction => transaction.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(transaction => transaction.OriginalTransaction)
            .WithMany()
            .HasForeignKey(transaction => transaction.OriginalTransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<RecurrenceTemplate>()
            .WithMany()
            .HasForeignKey(transaction => transaction.RecurrenceTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(transaction => transaction.AccountId)
            .HasDatabaseName("ix_transactions_account_id");

        builder.HasIndex(transaction => transaction.CategoryId)
            .HasDatabaseName("ix_transactions_category_id");

        builder.HasIndex(transaction => transaction.InstallmentGroupId)
            .HasDatabaseName("ix_transactions_installment_group")
            .HasFilter("installment_group_id IS NOT NULL");

        builder.HasIndex(transaction => transaction.TransferGroupId)
            .HasDatabaseName("ix_transactions_transfer_group")
            .HasFilter("transfer_group_id IS NOT NULL");

        builder.HasIndex(transaction => transaction.OperationId)
            .HasDatabaseName("ix_transactions_operation_id")
            .HasFilter("operation_id IS NOT NULL");

        builder.HasIndex(transaction => new { transaction.Status, transaction.DueDate })
            .HasDatabaseName("ix_transactions_status_due_date")
            .HasFilter("status = 2");

        builder.HasIndex(transaction => new { transaction.AccountId, transaction.CompetenceDate, transaction.Status })
            .HasDatabaseName("idx_transactions_account_competence_status");
    }
}
