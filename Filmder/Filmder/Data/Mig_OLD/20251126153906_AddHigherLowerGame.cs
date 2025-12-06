using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Filmder.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHigherLowerGame : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Comment",
                table: "SharedMovies",
                type: "TEXT",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.CreateTable(
                name: "HigherLowerGames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    CurrentStreak = table.Column<int>(type: "INTEGER", nullable: false),
                    BestStreak = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentMovieId = table.Column<int>(type: "INTEGER", nullable: true),
                    NextMovieId = table.Column<int>(type: "INTEGER", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HigherLowerGames", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HigherLowerGames_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HigherLowerGames_Movies_CurrentMovieId",
                        column: x => x.CurrentMovieId,
                        principalTable: "Movies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HigherLowerGames_Movies_NextMovieId",
                        column: x => x.NextMovieId,
                        principalTable: "Movies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HigherLowerGuesses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GameId = table.Column<int>(type: "INTEGER", nullable: false),
                    Movie1Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Movie2Id = table.Column<int>(type: "INTEGER", nullable: false),
                    GuessedHigher = table.Column<string>(type: "TEXT", nullable: false),
                    WasCorrect = table.Column<bool>(type: "INTEGER", nullable: false),
                    GuessedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HigherLowerGuesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HigherLowerGuesses_HigherLowerGames_GameId",
                        column: x => x.GameId,
                        principalTable: "HigherLowerGames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HigherLowerGuesses_Movies_Movie1Id",
                        column: x => x.Movie1Id,
                        principalTable: "Movies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HigherLowerGuesses_Movies_Movie2Id",
                        column: x => x.Movie2Id,
                        principalTable: "Movies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HigherLowerGames_CurrentMovieId",
                table: "HigherLowerGames",
                column: "CurrentMovieId");

            migrationBuilder.CreateIndex(
                name: "IX_HigherLowerGames_NextMovieId",
                table: "HigherLowerGames",
                column: "NextMovieId");

            migrationBuilder.CreateIndex(
                name: "IX_HigherLowerGames_UserId",
                table: "HigherLowerGames",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_HigherLowerGuesses_GameId",
                table: "HigherLowerGuesses",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_HigherLowerGuesses_Movie1Id",
                table: "HigherLowerGuesses",
                column: "Movie1Id");

            migrationBuilder.CreateIndex(
                name: "IX_HigherLowerGuesses_Movie2Id",
                table: "HigherLowerGuesses",
                column: "Movie2Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HigherLowerGuesses");

            migrationBuilder.DropTable(
                name: "HigherLowerGames");

            migrationBuilder.AlterColumn<string>(
                name: "Comment",
                table: "SharedMovies",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 200,
                oldNullable: true);
        }
    }
}
