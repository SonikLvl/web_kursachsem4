using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web_kursachsem4.Data.Migrations
{
    /// <inheritdoc />
    public partial class Final : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "Scores",
                type: "character varying(100)",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Users_UserName",
                table: "Users",
                column: "UserName");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserName",
                table: "Users",
                column: "UserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Scores_UserName",
                table: "Scores",
                column: "UserName",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Scores_Users_UserName",
                table: "Scores",
                column: "UserName",
                principalTable: "Users",
                principalColumn: "UserName",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Scores_Users_UserName",
                table: "Scores");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Users_UserName",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_UserName",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Scores_UserName",
                table: "Scores");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "Scores");
        }
    }
}
