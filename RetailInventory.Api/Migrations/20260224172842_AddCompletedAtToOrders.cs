using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetailInventory.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCompletedAtToOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "orders");
        }
    }
}
