using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Merchant.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EncryptWebhookSecretsWithDualRotation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "WebhookSecret",
                table: "Merchants",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviousWebhookSecret",
                table: "Merchants",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WebhookSecretRotatedAtUtc",
                table: "Merchants",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreviousWebhookSecret",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "WebhookSecretRotatedAtUtc",
                table: "Merchants");

            migrationBuilder.AlterColumn<string>(
                name: "WebhookSecret",
                table: "Merchants",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);
        }
    }
}
