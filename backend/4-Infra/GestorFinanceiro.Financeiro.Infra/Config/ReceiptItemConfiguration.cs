using GestorFinanceiro.Financeiro.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorFinanceiro.Financeiro.Infra.Config;

public class ReceiptItemConfiguration : IEntityTypeConfiguration<ReceiptItem>
{
    public void Configure(EntityTypeBuilder<ReceiptItem> builder)
    {
        builder.ToTable("receipt_items");

        builder.HasKey(receiptItem => receiptItem.Id);

        builder.Property(receiptItem => receiptItem.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(receiptItem => receiptItem.TransactionId)
            .HasColumnName("transaction_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(receiptItem => receiptItem.Description)
            .HasColumnName("description")
            .HasColumnType("varchar(500)")
            .IsRequired();

        builder.Property(receiptItem => receiptItem.ProductCode)
            .HasColumnName("product_code")
            .HasColumnType("varchar(100)");

        builder.Property(receiptItem => receiptItem.Quantity)
            .HasColumnName("quantity")
            .HasColumnType("numeric(18,4)")
            .IsRequired();

        builder.Property(receiptItem => receiptItem.UnitOfMeasure)
            .HasColumnName("unit_of_measure")
            .HasColumnType("varchar(20)")
            .IsRequired();

        builder.Property(receiptItem => receiptItem.UnitPrice)
            .HasColumnName("unit_price")
            .HasColumnType("numeric(18,4)")
            .IsRequired();

        builder.Property(receiptItem => receiptItem.TotalPrice)
            .HasColumnName("total_price")
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(receiptItem => receiptItem.ItemOrder)
            .HasColumnName("item_order")
            .HasColumnType("smallint")
            .HasConversion<short>()
            .IsRequired();

        builder.Property(receiptItem => receiptItem.CreatedBy)
            .HasColumnName("created_by")
            .HasColumnType("varchar(100)")
            .IsRequired();

        builder.Property(receiptItem => receiptItem.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(receiptItem => receiptItem.UpdatedBy)
            .HasColumnName("updated_by")
            .HasColumnType("varchar(100)");

        builder.Property(receiptItem => receiptItem.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        builder.HasOne(receiptItem => receiptItem.Transaction)
            .WithMany()
            .HasForeignKey(receiptItem => receiptItem.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(receiptItem => receiptItem.TransactionId)
            .HasDatabaseName("ix_receipt_items_transaction_id");
    }
}
