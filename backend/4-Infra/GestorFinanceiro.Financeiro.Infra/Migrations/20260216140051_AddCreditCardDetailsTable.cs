using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GestorFinanceiro.Financeiro.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditCardDetailsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "credit_card_details",
                columns: table => new
                {
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    credit_limit = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    closing_day = table.Column<short>(type: "smallint", nullable: false),
                    due_day = table.Column<short>(type: "smallint", nullable: false),
                    debit_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    enforce_credit_limit = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credit_card_details", x => x.account_id);
                    table.ForeignKey(
                        name: "FK_credit_card_details_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_credit_card_details_accounts_debit_account_id",
                        column: x => x.debit_account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_transactions_account_competence_status",
                table: "transactions",
                columns: new[] { "account_id", "competence_date", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_credit_card_details_debit_account_id",
                table: "credit_card_details",
                column: "debit_account_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "credit_card_details");

            migrationBuilder.DropIndex(
                name: "idx_transactions_account_competence_status",
                table: "transactions");
        }
    }
}
