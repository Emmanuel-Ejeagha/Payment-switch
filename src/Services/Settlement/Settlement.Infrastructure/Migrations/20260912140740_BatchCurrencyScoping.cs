using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Settlement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BatchCurrencyScoping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SettlementBatches_BatchDate",
                table: "SettlementBatches");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "SettlementBatches",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            // Backfill pre-existing batches from their first payout so the
            // composite unique index below builds cleanly and history stays
            // correctly scoped. Batches without payouts keep NULL currency.
            migrationBuilder.Sql(
                "UPDATE \"SettlementBatches\" b SET \"Currency\" = (SELECT p.\"Currency\" FROM \"Payouts\" p WHERE p.\"SettlementBatchId\" = b.\"Id\" LIMIT 1) WHERE b.\"Currency\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementBatches_BatchDate_Currency",
                table: "SettlementBatches",
                columns: new[] { "BatchDate", "Currency" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SettlementBatches_BatchDate_Currency",
                table: "SettlementBatches");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "SettlementBatches");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementBatches_BatchDate",
                table: "SettlementBatches",
                column: "BatchDate",
                unique: true);
        }
    }
}
