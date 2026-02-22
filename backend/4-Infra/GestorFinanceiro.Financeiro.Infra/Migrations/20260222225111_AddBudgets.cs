using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorFinanceiro.Financeiro.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddBudgets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "budgets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "varchar(150)", nullable: false),
                    percentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    reference_year = table.Column<short>(type: "smallint", nullable: false),
                    reference_month = table.Column<short>(type: "smallint", nullable: false),
                    is_recurrent = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_by = table.Column<string>(type: "varchar(100)", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_budgets", x => x.id);
                    table.CheckConstraint("ck_budgets_percentage_range", "percentage > 0 AND percentage <= 100");
                    table.CheckConstraint("ck_budgets_reference_month_range", "reference_month >= 1 AND reference_month <= 12");
                });

            migrationBuilder.CreateTable(
                name: "budget_categories",
                columns: table => new
                {
                    budget_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reference_year = table.Column<short>(type: "smallint", nullable: false),
                    reference_month = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_budget_categories", x => new { x.budget_id, x.category_id });
                    table.ForeignKey(
                        name: "FK_budget_categories_budgets_budget_id",
                        column: x => x.budget_id,
                        principalTable: "budgets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_budget_categories_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_budget_categories_category_reference",
                table: "budget_categories",
                columns: new[] { "category_id", "reference_year", "reference_month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_budgets_reference",
                table: "budgets",
                columns: new[] { "reference_year", "reference_month" });

            migrationBuilder.CreateIndex(
                name: "ux_budgets_name",
                table: "budgets",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "budget_categories");

            migrationBuilder.DropTable(
                name: "budgets");
        }
    }
}
