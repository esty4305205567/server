using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameHub.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDisplayMs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DisplayMs",
                table: "GameSessions",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayMs",
                table: "GameSessions");
        }
    }
}
