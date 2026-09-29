using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Settlement.Infrastructure.Migrations
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_Processed_LeaseExpiresAt",
                table: "OutboxMessages");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Processed",
                table: "OutboxMessages",
                column: "Processed");
        }
    }
}
