using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OperationIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_PaymentIntentId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_PaymentIntents_IdempotencyKey",
                table: "PaymentIntents");

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "Transactions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "IdempotencyKey",
                table: "PaymentIntents",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_PaymentIntentId_IdempotencyKey",
                table: "Transactions",
                columns: new[] { "PaymentIntentId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentIntents_MerchantId_IdempotencyKey",
                table: "PaymentIntents",
                columns: new[] { "MerchantId", "IdempotencyKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_PaymentIntentId_IdempotencyKey",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_PaymentIntents_MerchantId_IdempotencyKey",
                table: "PaymentIntents");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "Transactions");

            migrationBuilder.AlterColumn<string>(
                name: "IdempotencyKey",
                table: "PaymentIntents",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_PaymentIntentId",
                table: "Transactions",
                column: "PaymentIntentId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentIntents_IdempotencyKey",
                table: "PaymentIntents",
                column: "IdempotencyKey",
                unique: true);
        }
    }
}
