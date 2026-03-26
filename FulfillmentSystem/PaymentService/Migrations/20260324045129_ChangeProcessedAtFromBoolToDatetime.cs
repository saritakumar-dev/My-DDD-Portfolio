using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaymentService.Migrations
{
    /// <inheritdoc />
    public partial class ChangeProcessedAtFromBoolToDatetime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Processed",
                table: "InboxMessages");

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessedAt",
                table: "InboxMessages",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProcessedAt",
                table: "InboxMessages");

            migrationBuilder.AddColumn<bool>(
                name: "Processed",
                table: "InboxMessages",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }
    }
}
