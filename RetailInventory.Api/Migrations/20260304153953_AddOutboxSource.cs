using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetailInventory.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "source",
                table: "outbox_messages",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "source",
                table: "outbox_messages");
        }
    }
}
