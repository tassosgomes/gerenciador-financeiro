using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorFinanceiro.Financeiro.Infra.Config;

public class RecurrenceTemplateConfiguration : IEntityTypeConfiguration<RecurrenceTemplate>
{
    public void Configure(EntityTypeBuilder<RecurrenceTemplate> builder)
    {
        builder.ToTable(
            "recurrence_templates",
            tableBuilder => tableBuilder.HasCheckConstraint(
                "ck_recurrence_templates_day_of_month",
                "day_of_month BETWEEN 1 AND 31"));

        builder.HasKey(template => template.Id);

        builder.Property(template => template.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(template => template.AccountId)
            .HasColumnName("account_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(template => template.CategoryId)
            .HasColumnName("category_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(template => template.Type)
            .HasColumnName("type")
            .HasColumnType("smallint")
            .HasConversion<short>()
            .IsRequired();

        builder.Property(template => template.Amount)
            .HasColumnName("amount")
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(template => template.Description)
            .HasColumnName("description")
            .HasColumnType("varchar(500)")
            .IsRequired();

        builder.Property(template => template.DayOfMonth)
            .HasColumnName("day_of_month")
            .HasColumnType("smallint")
            .HasConversion(value => (short)value, value => (int)value)
            .IsRequired();

        builder.Property(template => template.IsActive)
            .HasColumnName("is_active")
            .HasColumnType("boolean")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(template => template.LastGeneratedDate)
            .HasColumnName("last_generated_date")
            .HasColumnType("date");

        builder.Property(template => template.DefaultStatus)
            .HasColumnName("default_status")
            .HasColumnType("smallint")
            .HasConversion<short>()
            .HasDefaultValue(TransactionStatus.Pending)
            .IsRequired();

        builder.Property(template => template.CreatedBy)
            .HasColumnName("created_by")
            .HasColumnType("varchar(100)")
            .IsRequired();

        builder.Property(template => template.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(template => template.UpdatedBy)
            .HasColumnName("updated_by")
            .HasColumnType("varchar(100)");

        builder.Property(template => template.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(template => template.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(template => template.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
