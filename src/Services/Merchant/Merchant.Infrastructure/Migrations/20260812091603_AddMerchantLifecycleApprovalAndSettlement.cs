using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Merchant.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMerchantLifecycleApprovalAndSettlement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContactAddress",
                table: "Merchants",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPerson",
                table: "Merchants",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPhone",
                table: "Merchants",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Merchants",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SettlementBankAccountName",
                table: "Merchants",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SettlementBankAccountNumber",
                table: "Merchants",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SettlementBankName",
                table: "Merchants",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SettlementCurrency",
                table: "Merchants",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SettlementSchedule",
                table: "Merchants",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContactAddress",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "ContactPerson",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "ContactPhone",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "SettlementBankAccountName",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "SettlementBankAccountNumber",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "SettlementBankName",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "SettlementCurrency",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "SettlementSchedule",
                table: "Merchants");
        }
    }
}
