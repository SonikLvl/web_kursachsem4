using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web_kursachsem4.Data.Migrations
{
    /// <inheritdoc />
    public partial class Finalfs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Levels");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Levels",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CompletedLevels = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Levels", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Levels_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });
        }
    }
}
