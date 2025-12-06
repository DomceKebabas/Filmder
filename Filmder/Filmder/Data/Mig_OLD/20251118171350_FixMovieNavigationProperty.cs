using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Filmder.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixMovieNavigationProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovieRatingGuess_Movies_MovieId",
                table: "MovieRatingGuess");

            migrationBuilder.DropForeignKey(
                name: "FK_MovieRatingGuess_RatingGuessingGames_GuessRatingGameId",
                table: "MovieRatingGuess");

            migrationBuilder.DropIndex(
                name: "IX_MovieRatingGuess_GuessRatingGameId",
                table: "MovieRatingGuess");

            migrationBuilder.DropColumn(
                name: "GuessRatingGameId",
                table: "MovieRatingGuess");

            migrationBuilder.AddColumn<int>(
                name: "MovieId1",
                table: "MovieRatingGuess",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_MovieRatingGuess_MovieId1",
                table: "MovieRatingGuess",
                column: "MovieId1");

            migrationBuilder.AddForeignKey(
                name: "FK_MovieRatingGuess_Movies_MovieId1",
                table: "MovieRatingGuess",
                column: "MovieId1",
                principalTable: "Movies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MovieRatingGuess_RatingGuessingGames_MovieId",
                table: "MovieRatingGuess",
                column: "MovieId",
                principalTable: "RatingGuessingGames",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovieRatingGuess_Movies_MovieId1",
                table: "MovieRatingGuess");

            migrationBuilder.DropForeignKey(
                name: "FK_MovieRatingGuess_RatingGuessingGames_MovieId",
                table: "MovieRatingGuess");

            migrationBuilder.DropIndex(
                name: "IX_MovieRatingGuess_MovieId1",
                table: "MovieRatingGuess");

            migrationBuilder.DropColumn(
                name: "MovieId1",
                table: "MovieRatingGuess");

            migrationBuilder.AddColumn<int>(
                name: "GuessRatingGameId",
                table: "MovieRatingGuess",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MovieRatingGuess_GuessRatingGameId",
                table: "MovieRatingGuess",
                column: "GuessRatingGameId");

            migrationBuilder.AddForeignKey(
                name: "FK_MovieRatingGuess_Movies_MovieId",
                table: "MovieRatingGuess",
                column: "MovieId",
                principalTable: "Movies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MovieRatingGuess_RatingGuessingGames_GuessRatingGameId",
                table: "MovieRatingGuess",
                column: "GuessRatingGameId",
                principalTable: "RatingGuessingGames",
                principalColumn: "Id");
        }
    }
}
