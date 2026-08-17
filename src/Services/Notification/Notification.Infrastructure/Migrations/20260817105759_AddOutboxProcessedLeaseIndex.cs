using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notification.Infrastructure.Migrations
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
                name: "IX_Notifications_Status_NextRetryAt",
                table: "Notifications",
                columns: new[] { "Status", "NextRetryAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_Processed_LeaseExpiresAt",
                table: "OutboxMessages");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_Status_NextRetryAt",
                table: "Notifications");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Processed",
                table: "OutboxMessages",
                column: "Processed");
        }
    }
}
