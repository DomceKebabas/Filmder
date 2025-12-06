using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Filmder.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupNavigationToGame : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Games",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "RatingGuessingGames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GroupId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RatingGuessingGames", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RatingGuessingGames_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RatingGuessingGames_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SharedMovies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GroupId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserWhoAddedId = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    MovieId = table.Column<int>(type: "INTEGER", nullable: false),
                    Comment = table.Column<string>(type: "TEXT", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharedMovies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SharedMovies_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SharedMovies_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SharedMovies_Movies_MovieId",
                        column: x => x.MovieId,
                        principalTable: "Movies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuessRatingGameMovies",
                columns: table => new
                {
                    GuessRatingGameId = table.Column<int>(type: "INTEGER", nullable: false),
                    MoviesId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuessRatingGameMovies", x => new { x.GuessRatingGameId, x.MoviesId });
                    table.ForeignKey(
                        name: "FK_GuessRatingGameMovies_Movies_MoviesId",
                        column: x => x.MoviesId,
                        principalTable: "Movies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GuessRatingGameMovies_RatingGuessingGames_GuessRatingGameId",
                        column: x => x.GuessRatingGameId,
                        principalTable: "RatingGuessingGames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MovieRatingGuess",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    MovieId = table.Column<int>(type: "INTEGER", nullable: false),
                    RatingGuessValue = table.Column<int>(type: "INTEGER", nullable: false),
                    GuessRatingGameId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovieRatingGuess", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovieRatingGuess_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MovieRatingGuess_Movies_MovieId",
                        column: x => x.MovieId,
                        principalTable: "Movies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MovieRatingGuess_RatingGuessingGames_GuessRatingGameId",
                        column: x => x.GuessRatingGameId,
                        principalTable: "RatingGuessingGames",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Games_GroupId",
                table: "Games",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_GuessRatingGameMovies_MoviesId",
                table: "GuessRatingGameMovies",
                column: "MoviesId");

            migrationBuilder.CreateIndex(
                name: "IX_MovieRatingGuess_GuessRatingGameId",
                table: "MovieRatingGuess",
                column: "GuessRatingGameId");

            migrationBuilder.CreateIndex(
                name: "IX_MovieRatingGuess_MovieId",
                table: "MovieRatingGuess",
                column: "MovieId");

            migrationBuilder.CreateIndex(
                name: "IX_MovieRatingGuess_UserId",
                table: "MovieRatingGuess",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RatingGuessingGames_GroupId",
                table: "RatingGuessingGames",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_RatingGuessingGames_UserId",
                table: "RatingGuessingGames",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedMovies_GroupId",
                table: "SharedMovies",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedMovies_MovieId",
                table: "SharedMovies",
                column: "MovieId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedMovies_UserId",
                table: "SharedMovies",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Games_Groups_GroupId",
                table: "Games",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_Groups_GroupId",
                table: "Games");

            migrationBuilder.DropTable(
                name: "GuessRatingGameMovies");

            migrationBuilder.DropTable(
                name: "MovieRatingGuess");

            migrationBuilder.DropTable(
                name: "SharedMovies");

            migrationBuilder.DropTable(
                name: "RatingGuessingGames");

            migrationBuilder.DropIndex(
                name: "IX_Games_GroupId",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Games");
        }
    }
}
