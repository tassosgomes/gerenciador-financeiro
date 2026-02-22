using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Infra.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorFinanceiro.Financeiro.Infra.Config;

public class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.ToTable(
            "budgets",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint("ck_budgets_percentage_range", "percentage > 0 AND percentage <= 100");
                tableBuilder.HasCheckConstraint("ck_budgets_reference_month_range", "reference_month >= 1 AND reference_month <= 12");
            });

        builder.HasKey(budget => budget.Id);

        builder.Property(budget => budget.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(budget => budget.Name)
            .HasColumnName("name")
            .HasColumnType("varchar(150)")
            .IsRequired();

        builder.Property(budget => budget.Percentage)
            .HasColumnName("percentage")
            .HasColumnType("numeric(5,2)")
            .IsRequired();

        builder.Property(budget => budget.ReferenceYear)
            .HasColumnName("reference_year")
            .HasColumnType("smallint")
            .HasConversion<short>()
            .IsRequired();

        builder.Property(budget => budget.ReferenceMonth)
            .HasColumnName("reference_month")
            .HasColumnType("smallint")
            .HasConversion<short>()
            .IsRequired();

        builder.Property(budget => budget.IsRecurrent)
            .HasColumnName("is_recurrent")
            .HasColumnType("boolean")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(budget => budget.CreatedBy)
            .HasColumnName("created_by")
            .HasColumnType("varchar(100)")
            .IsRequired();

        builder.Property(budget => budget.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(budget => budget.UpdatedBy)
            .HasColumnName("updated_by")
            .HasColumnType("varchar(100)");

        builder.Property(budget => budget.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(budget => budget.Name)
            .IsUnique()
            .HasDatabaseName("ux_budgets_name");

        builder.HasIndex(budget => new { budget.ReferenceYear, budget.ReferenceMonth })
            .HasDatabaseName("ix_budgets_reference");

        builder.Ignore(budget => budget.CategoryIds);
    }
}

public class BudgetCategoryLinkConfiguration : IEntityTypeConfiguration<BudgetCategoryLink>
{
    public void Configure(EntityTypeBuilder<BudgetCategoryLink> builder)
    {
        builder.ToTable("budget_categories");

        builder.HasKey(link => new { link.BudgetId, link.CategoryId });

        builder.Property(link => link.BudgetId)
            .HasColumnName("budget_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(link => link.CategoryId)
            .HasColumnName("category_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(link => link.ReferenceYear)
            .HasColumnName("reference_year")
            .HasColumnType("smallint")
            .IsRequired();

        builder.Property(link => link.ReferenceMonth)
            .HasColumnName("reference_month")
            .HasColumnType("smallint")
            .IsRequired();

        builder.HasOne<Budget>()
            .WithMany()
            .HasForeignKey(link => link.BudgetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(link => link.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(link => new { link.CategoryId, link.ReferenceYear, link.ReferenceMonth })
            .IsUnique()
            .HasDatabaseName("ux_budget_categories_category_reference");
    }
}