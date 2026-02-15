using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorFinanceiro.Financeiro.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddIsSystemToCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add is_system column with default false
            migrationBuilder.AddColumn<bool>(
                name: "is_system",
                table: "categories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Update existing seed categories to be system categories
            migrationBuilder.Sql(@"
                UPDATE categories 
                SET is_system = true 
                WHERE id IN (
                    '00000000-0000-0000-0000-000000000001',
                    '00000000-0000-0000-0000-000000000002',
                    '00000000-0000-0000-0000-000000000003',
                    '00000000-0000-0000-0000-000000000004',
                    '00000000-0000-0000-0000-000000000005',
                    '00000000-0000-0000-0000-000000000006',
                    '00000000-0000-0000-0000-000000000007',
                    '00000000-0000-0000-0000-000000000008',
                    '00000000-0000-0000-0000-000000000009',
                    '00000000-0000-0000-0000-000000000010',
                    '00000000-0000-0000-0000-000000000011',
                    '00000000-0000-0000-0000-000000000012'
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_system",
                table: "categories");
        }
    }
}
