using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ledger.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LedgerIntegrityConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_LedgerAccounts_MerchantId_Currency",
                table: "LedgerAccounts",
                columns: new[] { "MerchantId", "Currency" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_LedgerAccounts_NonNegativeBalances",
                table: "LedgerAccounts",
                sql: "\"AvailableBalance\" >= 0 AND \"PendingBalance\" >= 0 AND \"ReservedBalance\" >= 0");

            // Journal entries are an immutable audit trail: the DB must reject
            // deleting an account that still has entries (RESTRICT), not silently
            // cascade them away. EF Core cannot express ON DELETE RESTRICT for an
            // owned relationship in its model snapshot, so it is enforced here.
            migrationBuilder.Sql(
                """
                ALTER TABLE "JournalEntries"
                DROP CONSTRAINT "FK_JournalEntries_LedgerAccounts_LedgerAccountId";

                ALTER TABLE "JournalEntries"
                ADD CONSTRAINT "FK_JournalEntries_LedgerAccounts_LedgerAccountId"
                FOREIGN KEY ("LedgerAccountId")
                REFERENCES "LedgerAccounts" ("Id")
                ON DELETE RESTRICT;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "JournalEntries"
                DROP CONSTRAINT "FK_JournalEntries_LedgerAccounts_LedgerAccountId";

                ALTER TABLE "JournalEntries"
                ADD CONSTRAINT "FK_JournalEntries_LedgerAccounts_LedgerAccountId"
                FOREIGN KEY ("LedgerAccountId")
                REFERENCES "LedgerAccounts" ("Id")
                ON DELETE CASCADE;
                """);

            migrationBuilder.DropIndex(
                name: "IX_LedgerAccounts_MerchantId_Currency",
                table: "LedgerAccounts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LedgerAccounts_NonNegativeBalances",
                table: "LedgerAccounts");
        }
    }
}
