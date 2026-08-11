using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Settlement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UniqueBatchDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SettlementBatches_BatchDate",
                table: "SettlementBatches",
                column: "BatchDate",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SettlementBatches_BatchDate",
                table: "SettlementBatches");
        }
    }
}
