using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpDesk.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReopeningFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastReopenedAt",
                table: "Tickets",
                type: "datetime(6)",
                nullable: true);
            migrationBuilder.AddColumn<Guid>(
               name: "ReopenedById",
               table: "Tickets",
               type: "char(36)",
               nullable: true,
               collation: "ascii_general_ci");
            migrationBuilder.AddColumn<int>(
                name: "ReopenCount",
                table: "Tickets",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastReopenedAt",
                table: "Tickets");

            migrationBuilder.DropColumn(
              name: "ReopenedById",
              table: "Tickets");

            migrationBuilder.DropColumn(
              name: "ReopenCount",
              table: "Tickets");
        }
    }
}
