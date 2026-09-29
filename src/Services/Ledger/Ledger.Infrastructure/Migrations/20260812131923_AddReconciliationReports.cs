using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ledger.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReconciliationReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReconciliationReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RunAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    TotalAccounts = table.Column<int>(type: "integer", nullable: false),
                    MismatchCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconciliationReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReconciliationItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MerchantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    ExpectedAvailable = table.Column<long>(type: "bigint", nullable: false),
                    ActualAvailable = table.Column<long>(type: "bigint", nullable: false),
                    ExpectedPending = table.Column<long>(type: "bigint", nullable: false),
                    ActualPending = table.Column<long>(type: "bigint", nullable: false),
                    ExpectedReserved = table.Column<long>(type: "bigint", nullable: false),
                    ActualReserved = table.Column<long>(type: "bigint", nullable: false),
                    IsMatch = table.Column<bool>(type: "boolean", nullable: false),
                    ReconciliationReportId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconciliationItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReconciliationItems_ReconciliationReports_ReconciliationRep~",
                        column: x => x.ReconciliationReportId,
                        principalTable: "ReconciliationReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationItems_ReconciliationReportId",
                table: "ReconciliationItems",
                column: "ReconciliationReportId");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationReports_RunAtUtc",
                table: "ReconciliationReports",
                column: "RunAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReconciliationItems");

            migrationBuilder.DropTable(
                name: "ReconciliationReports");
        }
    }
}
