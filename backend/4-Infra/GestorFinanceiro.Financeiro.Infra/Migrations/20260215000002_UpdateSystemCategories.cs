using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorFinanceiro.Financeiro.Infra.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSystemCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename "Investimento" to "Investimentos" (ID 11)
            migrationBuilder.Sql(@"
                UPDATE categories 
                SET name = 'Investimentos', updated_by = 'system', updated_at = NOW()
                WHERE id = '00000000-0000-0000-0000-000000000011';
            ");

            // Insert new system categories: Serviços (ID 13) and Impostos (ID 14)
            migrationBuilder.Sql(@"
                INSERT INTO categories (id, created_at, created_by, is_active, is_system, name, type, updated_at, updated_by)
                VALUES
                    ('00000000-0000-0000-0000-000000000013', '2025-01-01T00:00:00Z', 'system', true, true, 'Serviços', 2, NULL, NULL),
                    ('00000000-0000-0000-0000-000000000014', '2025-01-01T00:00:00Z', 'system', true, true, 'Impostos', 2, NULL, NULL)
                ON CONFLICT (id) DO NOTHING;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert "Investimentos" back to "Investimento"
            migrationBuilder.Sql(@"
                UPDATE categories 
                SET name = 'Investimento', updated_by = NULL, updated_at = NULL
                WHERE id = '00000000-0000-0000-0000-000000000011';
            ");

            // Delete the new categories
            migrationBuilder.Sql(@"
                DELETE FROM categories
                WHERE id IN (
                    '00000000-0000-0000-0000-000000000013',
                    '00000000-0000-0000-0000-000000000014'
                );
            ");
        }
    }
}
