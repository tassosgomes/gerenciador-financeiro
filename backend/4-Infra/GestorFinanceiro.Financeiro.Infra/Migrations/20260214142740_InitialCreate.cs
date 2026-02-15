using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorFinanceiro.Financeiro.Infra.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Enable pgcrypto extension required for digest() function
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pgcrypto;");

            migrationBuilder.CreateTable(
                name: "accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "varchar(150)", nullable: false),
                    type = table.Column<short>(type: "smallint", nullable: false),
                    balance = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    allow_negative_balance = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_by = table.Column<string>(type: "varchar(100)", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accounts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "varchar(150)", nullable: false),
                    type = table.Column<short>(type: "smallint", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_by = table.Column<string>(type: "varchar(100)", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "operation_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    operation_id = table.Column<string>(type: "varchar(100)", nullable: false),
                    operation_type = table.Column<string>(type: "varchar(50)", nullable: false),
                    result_entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    result_payload = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_operation_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "recurrence_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<short>(type: "smallint", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    description = table.Column<string>(type: "varchar(500)", nullable: false),
                    day_of_month = table.Column<short>(type: "smallint", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    last_generated_date = table.Column<DateTime>(type: "date", nullable: true),
                    default_status = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)2),
                    created_by = table.Column<string>(type: "varchar(100)", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recurrence_templates", x => x.id);
                    table.CheckConstraint("ck_recurrence_templates_day_of_month", "day_of_month BETWEEN 1 AND 31");
                    table.ForeignKey(
                        name: "FK_recurrence_templates_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_recurrence_templates_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<short>(type: "smallint", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    description = table.Column<string>(type: "varchar(500)", nullable: false),
                    competence_date = table.Column<DateTime>(type: "date", nullable: false),
                    due_date = table.Column<DateTime>(type: "date", nullable: true),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    is_adjustment = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    original_transaction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    has_adjustment = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    installment_group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    installment_number = table.Column<short>(type: "smallint", nullable: true),
                    total_installments = table.Column<short>(type: "smallint", nullable: true),
                    is_recurrent = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    recurrence_template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    transfer_group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cancellation_reason = table.Column<string>(type: "varchar(500)", nullable: true),
                    cancelled_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    operation_id = table.Column<string>(type: "varchar(100)", nullable: true),
                    created_by = table.Column<string>(type: "varchar(100)", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transactions", x => x.id);
                    table.CheckConstraint("ck_transactions_amount_positive", "amount > 0");
                    table.ForeignKey(
                        name: "FK_transactions_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transactions_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transactions_recurrence_templates_recurrence_template_id",
                        column: x => x.recurrence_template_id,
                        principalTable: "recurrence_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transactions_transactions_original_transaction_id",
                        column: x => x.original_transaction_id,
                        principalTable: "transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_operation_logs_expires_at",
                table: "operation_logs",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_operation_logs_operation_id",
                table: "operation_logs",
                column: "operation_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_recurrence_templates_account_id",
                table: "recurrence_templates",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_recurrence_templates_category_id",
                table: "recurrence_templates",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_transactions_account_id",
                table: "transactions",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_transactions_category_id",
                table: "transactions",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_transactions_installment_group",
                table: "transactions",
                column: "installment_group_id",
                filter: "installment_group_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_transactions_operation_id",
                table: "transactions",
                column: "operation_id",
                filter: "operation_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_original_transaction_id",
                table: "transactions",
                column: "original_transaction_id");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_recurrence_template_id",
                table: "transactions",
                column: "recurrence_template_id");

            migrationBuilder.CreateIndex(
                name: "ix_transactions_status_due_date",
                table: "transactions",
                columns: new[] { "status", "due_date" },
                filter: "status = 2");

            migrationBuilder.CreateIndex(
                name: "ix_transactions_transfer_group",
                table: "transactions",
                column: "transfer_group_id",
                filter: "transfer_group_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "operation_logs");

            migrationBuilder.DropTable(
                name: "transactions");

            migrationBuilder.DropTable(
                name: "recurrence_templates");

            migrationBuilder.DropTable(
                name: "accounts");

            migrationBuilder.DropTable(
                name: "categories");
        }
    }
}
