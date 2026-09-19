using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Zeymera.Scoreboard.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerSet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerSet", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClientSet",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CustomerId = table.Column<int>(type: "integer", nullable: true),
                    TableNumber = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientSet_CustomerSet_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "CustomerSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MatchStatSet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientId = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Player1ExternalId = table.Column<int>(type: "integer", nullable: true),
                    Player1Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Player1Score = table.Column<int>(type: "integer", nullable: false),
                    Player1Avg = table.Column<double>(type: "double precision", nullable: false),
                    Player1HighRun = table.Column<int>(type: "integer", nullable: false),
                    Player2ExternalId = table.Column<int>(type: "integer", nullable: true),
                    Player2Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Player2Score = table.Column<int>(type: "integer", nullable: false),
                    Player2Avg = table.Column<double>(type: "double precision", nullable: false),
                    Player2HighRun = table.Column<int>(type: "integer", nullable: false),
                    Inning = table.Column<int>(type: "integer", nullable: false),
                    MatchTarget = table.Column<int>(type: "integer", nullable: false),
                    Winner = table.Column<int>(type: "integer", nullable: false),
                    PlayedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchStatSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchStatSet_ClientSet_ClientId",
                        column: x => x.ClientId,
                        principalTable: "ClientSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerSet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientId = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ExternalId = table.Column<int>(type: "integer", nullable: false),
                    Nickname = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PhotoPath = table.Column<string>(type: "text", nullable: true),
                    AvatarId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerSet_ClientSet_ClientId",
                        column: x => x.ClientId,
                        principalTable: "ClientSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClientSet_CustomerId",
                table: "ClientSet",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchStatSet_ClientId",
                table: "MatchStatSet",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSet_ClientId_ExternalId",
                table: "PlayerSet",
                columns: new[] { "ClientId", "ExternalId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchStatSet");

            migrationBuilder.DropTable(
                name: "PlayerSet");

            migrationBuilder.DropTable(
                name: "ClientSet");

            migrationBuilder.DropTable(
                name: "CustomerSet");
        }
    }
}
