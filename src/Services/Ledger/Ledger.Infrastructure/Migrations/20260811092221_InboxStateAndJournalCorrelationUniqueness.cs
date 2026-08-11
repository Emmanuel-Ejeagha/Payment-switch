using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ledger.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InboxStateAndJournalCorrelationUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "State",
                table: "InboxMessages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Backfill: rows already marked processed must not be reclaimed as Processing.
            // NOTE: if an existing deployment holds duplicate JournalEntries.CorrelationId
            // values, CreateIndex below fails loudly and the duplicates must be resolved first.
            migrationBuilder.Sql(
                """
                UPDATE "InboxMessages"
                SET "State" = 1
                WHERE "ProcessedAt" IS NOT NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_CorrelationId",
                table: "JournalEntries",
                column: "CorrelationId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_JournalEntries_CorrelationId",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "State",
                table: "InboxMessages");
        }
    }
}
