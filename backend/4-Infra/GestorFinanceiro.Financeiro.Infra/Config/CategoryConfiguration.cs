using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorFinanceiro.Financeiro.Infra.Config;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        var createdAtUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        builder.ToTable("categories");

        builder.HasKey(category => category.Id);

        builder.Property(category => category.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(category => category.Name)
            .HasColumnName("name")
            .HasColumnType("varchar(150)")
            .IsRequired();

        builder.Property(category => category.Type)
            .HasColumnName("type")
            .HasColumnType("smallint")
            .HasConversion<short>()
            .IsRequired();

        builder.Property(category => category.IsActive)
            .HasColumnName("is_active")
            .HasColumnType("boolean")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(category => category.CreatedBy)
            .HasColumnName("created_by")
            .HasColumnType("varchar(100)")
            .IsRequired();

        builder.Property(category => category.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(category => category.UpdatedBy)
            .HasColumnName("updated_by")
            .HasColumnType("varchar(100)");

        builder.Property(category => category.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        builder.HasData(
            new
            {
                Id = new Guid("00000000-0000-0000-0000-000000000001"),
                Name = "Alimentação",
                Type = CategoryType.Despesa,
                IsActive = true,
                CreatedBy = "system",
                CreatedAt = createdAtUtc,
            },
            new
            {
                Id = new Guid("00000000-0000-0000-0000-000000000002"),
                Name = "Transporte",
                Type = CategoryType.Despesa,
                IsActive = true,
                CreatedBy = "system",
                CreatedAt = createdAtUtc,
            },
            new
            {
                Id = new Guid("00000000-0000-0000-0000-000000000003"),
                Name = "Moradia",
                Type = CategoryType.Despesa,
                IsActive = true,
                CreatedBy = "system",
                CreatedAt = createdAtUtc,
            },
            new
            {
                Id = new Guid("00000000-0000-0000-0000-000000000004"),
                Name = "Lazer",
                Type = CategoryType.Despesa,
                IsActive = true,
                CreatedBy = "system",
                CreatedAt = createdAtUtc,
            },
            new
            {
                Id = new Guid("00000000-0000-0000-0000-000000000005"),
                Name = "Saúde",
                Type = CategoryType.Despesa,
                IsActive = true,
                CreatedBy = "system",
                CreatedAt = createdAtUtc,
            },
            new
            {
                Id = new Guid("00000000-0000-0000-0000-000000000006"),
                Name = "Educação",
                Type = CategoryType.Despesa,
                IsActive = true,
                CreatedBy = "system",
                CreatedAt = createdAtUtc,
            },
            new
            {
                Id = new Guid("00000000-0000-0000-0000-000000000007"),
                Name = "Vestuário",
                Type = CategoryType.Despesa,
                IsActive = true,
                CreatedBy = "system",
                CreatedAt = createdAtUtc,
            },
            new
            {
                Id = new Guid("00000000-0000-0000-0000-000000000008"),
                Name = "Outros",
                Type = CategoryType.Despesa,
                IsActive = true,
                CreatedBy = "system",
                CreatedAt = createdAtUtc,
            },
            new
            {
                Id = new Guid("00000000-0000-0000-0000-000000000009"),
                Name = "Salário",
                Type = CategoryType.Receita,
                IsActive = true,
                CreatedBy = "system",
                CreatedAt = createdAtUtc,
            },
            new
            {
                Id = new Guid("00000000-0000-0000-0000-000000000010"),
                Name = "Freelance",
                Type = CategoryType.Receita,
                IsActive = true,
                CreatedBy = "system",
                CreatedAt = createdAtUtc,
            },
            new
            {
                Id = new Guid("00000000-0000-0000-0000-000000000011"),
                Name = "Investimento",
                Type = CategoryType.Receita,
                IsActive = true,
                CreatedBy = "system",
                CreatedAt = createdAtUtc,
            },
            new
            {
                Id = new Guid("00000000-0000-0000-0000-000000000012"),
                Name = "Outros",
                Type = CategoryType.Receita,
                IsActive = true,
                CreatedBy = "system",
                CreatedAt = createdAtUtc,
            });
    }
}
