using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetailInventory.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveExternalId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_products_external_id",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_customers_external_id",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "external_id",
                table: "products");

            migrationBuilder.DropColumn(
                name: "external_id",
                table: "customers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "external_id",
                table: "products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "external_id",
                table: "customers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_products_external_id",
                table: "products",
                column: "external_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_customers_external_id",
                table: "customers",
                column: "external_id",
                unique: true);
        }
    }
}
