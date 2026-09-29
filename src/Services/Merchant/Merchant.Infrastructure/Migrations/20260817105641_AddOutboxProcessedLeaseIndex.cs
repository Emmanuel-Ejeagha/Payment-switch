using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Merchant.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxProcessedLeaseIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_Processed",
                table: "OutboxMessages");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Processed_LeaseExpiresAt",
                table: "OutboxMessages",
                columns: new[] { "Processed", "LeaseExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Merchants_OwnerId",
                table: "Merchants",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Merchants_Status",
                table: "Merchants",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_Processed_LeaseExpiresAt",
                table: "OutboxMessages");

            migrationBuilder.DropIndex(
                name: "IX_Merchants_OwnerId",
                table: "Merchants");

            migrationBuilder.DropIndex(
                name: "IX_Merchants_Status",
                table: "Merchants");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Processed",
                table: "OutboxMessages",
                column: "Processed");
        }
    }
}
