using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Zeymera.Scoreboard.Api.Migrations
{
    /// <inheritdoc />
    public partial class RenameCustomerToClubAddTeams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClientSet_CustomerSet_CustomerId",
                table: "ClientSet");

            migrationBuilder.RenameTable(
                name: "CustomerSet",
                newName: "ClubSet");

            migrationBuilder.RenameColumn(
                name: "CustomerId",
                table: "ClientSet",
                newName: "ClubId");

            migrationBuilder.RenameIndex(
                name: "IX_ClientSet_CustomerId",
                table: "ClientSet",
                newName: "IX_ClientSet_ClubId");

            migrationBuilder.Sql("ALTER TABLE \"ClubSet\" RENAME CONSTRAINT \"PK_CustomerSet\" TO \"PK_ClubSet\";");

            migrationBuilder.CreateTable(
                name: "TeamSet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientId = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ExternalId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamSet_ClientSet_ClientId",
                        column: x => x.ClientId,
                        principalTable: "ClientSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamPlayerSet",
                columns: table => new
                {
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamPlayerSet", x => new { x.TeamId, x.PlayerId });
                    table.ForeignKey(
                        name: "FK_TeamPlayerSet_PlayerSet_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "PlayerSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamPlayerSet_TeamSet_TeamId",
                        column: x => x.TeamId,
                        principalTable: "TeamSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeamPlayerSet_PlayerId",
                table: "TeamPlayerSet",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamSet_ClientId_ExternalId",
                table: "TeamSet",
                columns: new[] { "ClientId", "ExternalId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ClientSet_ClubSet_ClubId",
                table: "ClientSet",
                column: "ClubId",
                principalTable: "ClubSet",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClientSet_ClubSet_ClubId",
                table: "ClientSet");

            migrationBuilder.DropTable(
                name: "TeamPlayerSet");

            migrationBuilder.DropTable(
                name: "TeamSet");

            migrationBuilder.Sql("ALTER TABLE \"ClubSet\" RENAME CONSTRAINT \"PK_ClubSet\" TO \"PK_CustomerSet\";");

            migrationBuilder.RenameIndex(
                name: "IX_ClientSet_ClubId",
                table: "ClientSet",
                newName: "IX_ClientSet_CustomerId");

            migrationBuilder.RenameColumn(
                name: "ClubId",
                table: "ClientSet",
                newName: "CustomerId");

            migrationBuilder.RenameTable(
                name: "ClubSet",
                newName: "CustomerSet");

            migrationBuilder.AddForeignKey(
                name: "FK_ClientSet_CustomerSet_CustomerId",
                table: "ClientSet",
                column: "CustomerId",
                principalTable: "CustomerSet",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
