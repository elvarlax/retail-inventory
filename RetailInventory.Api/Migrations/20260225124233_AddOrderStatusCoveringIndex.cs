using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetailInventory.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderStatusCoveringIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_orders_status",
                table: "orders",
                column: "status")
                .Annotation("Npgsql:IndexInclude", new[] { "total_amount" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_orders_status",
                table: "orders");
        }
    }
}
