using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameHub.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddItemSlot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Slot",
                table: "GameItems",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Slot",
                table: "GameItems");
        }
    }
}
