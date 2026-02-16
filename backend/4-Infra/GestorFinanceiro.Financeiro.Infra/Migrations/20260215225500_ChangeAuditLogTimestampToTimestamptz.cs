using System;
using GestorFinanceiro.Financeiro.Infra.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorFinanceiro.Financeiro.Infra.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(FinanceiroDbContext))]
    [Migration("20260215225500_ChangeAuditLogTimestampToTimestamptz")]
    public partial class ChangeAuditLogTimestampToTimestamptz : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE audit_logs ALTER COLUMN \"timestamp\" TYPE timestamp with time zone USING \"timestamp\" AT TIME ZONE 'UTC';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE audit_logs ALTER COLUMN \"timestamp\" TYPE timestamp USING \"timestamp\" AT TIME ZONE 'UTC';");
        }
    }
}
