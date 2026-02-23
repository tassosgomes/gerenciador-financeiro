using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorFinanceiro.Financeiro.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiptItemsAndEstablishments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "establishments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    transaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "varchar(300)", nullable: false),
                    cnpj = table.Column<string>(type: "varchar(14)", nullable: false),
                    access_key = table.Column<string>(type: "varchar(44)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(100)", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_establishments", x => x.id);
                    table.ForeignKey(
                        name: "FK_establishments_transactions_transaction_id",
                        column: x => x.transaction_id,
                        principalTable: "transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "receipt_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    transaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "varchar(500)", nullable: false),
                    product_code = table.Column<string>(type: "varchar(100)", nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unit_of_measure = table.Column<string>(type: "varchar(20)", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total_price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    item_order = table.Column<short>(type: "smallint", nullable: false),
                    created_by = table.Column<string>(type: "varchar(100)", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_receipt_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_receipt_items_transactions_transaction_id",
                        column: x => x.transaction_id,
                        principalTable: "transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_establishments_access_key",
                table: "establishments",
                column: "access_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_establishments_transaction_id",
                table: "establishments",
                column: "transaction_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_receipt_items_transaction_id",
                table: "receipt_items",
                column: "transaction_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "establishments");

            migrationBuilder.DropTable(
                name: "receipt_items");
        }
    }
}
