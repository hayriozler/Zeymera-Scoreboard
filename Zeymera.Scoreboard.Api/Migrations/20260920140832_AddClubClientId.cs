using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zeymera.Scoreboard.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddClubClientId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientId",
                table: "ClubSet",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_ClubSet_ClientId",
                table: "ClubSet",
                column: "ClientId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ClubSet_ClientId",
                table: "ClubSet");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "ClubSet");
        }
    }
}
