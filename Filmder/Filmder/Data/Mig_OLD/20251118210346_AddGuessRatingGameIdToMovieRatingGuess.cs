using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Filmder.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGuessRatingGameIdToMovieRatingGuess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovieRatingGuess_AspNetUsers_UserId",
                table: "MovieRatingGuess");

            migrationBuilder.DropForeignKey(
                name: "FK_MovieRatingGuess_Movies_MovieId1",
                table: "MovieRatingGuess");

            migrationBuilder.DropForeignKey(
                name: "FK_MovieRatingGuess_RatingGuessingGames_MovieId",
                table: "MovieRatingGuess");

            migrationBuilder.DropForeignKey(
                name: "FK_RatingGuessingGames_AspNetUsers_UserId",
                table: "RatingGuessingGames");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MovieRatingGuess",
                table: "MovieRatingGuess");

            migrationBuilder.DropIndex(
                name: "IX_MovieRatingGuess_MovieId1",
                table: "MovieRatingGuess");

            migrationBuilder.DropColumn(
                name: "MovieId1",
                table: "MovieRatingGuess");

            migrationBuilder.RenameTable(
                name: "MovieRatingGuess",
                newName: "MovieRatingGuesses");

            migrationBuilder.RenameIndex(
                name: "IX_MovieRatingGuess_UserId",
                table: "MovieRatingGuesses",
                newName: "IX_MovieRatingGuesses_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_MovieRatingGuess_MovieId",
                table: "MovieRatingGuesses",
                newName: "IX_MovieRatingGuesses_MovieId");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "RatingGuessingGames",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<double>(
                name: "RatingGuessValue",
                table: "MovieRatingGuesses",
                type: "REAL",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<int>(
                name: "GuessRatingGameId",
                table: "MovieRatingGuesses",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_MovieRatingGuesses",
                table: "MovieRatingGuesses",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_MovieRatingGuesses_GuessRatingGameId",
                table: "MovieRatingGuesses",
                column: "GuessRatingGameId");

            migrationBuilder.AddForeignKey(
                name: "FK_MovieRatingGuesses_AspNetUsers_UserId",
                table: "MovieRatingGuesses",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MovieRatingGuesses_Movies_MovieId",
                table: "MovieRatingGuesses",
                column: "MovieId",
                principalTable: "Movies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MovieRatingGuesses_RatingGuessingGames_GuessRatingGameId",
                table: "MovieRatingGuesses",
                column: "GuessRatingGameId",
                principalTable: "RatingGuessingGames",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_RatingGuessingGames_AspNetUsers_UserId",
                table: "RatingGuessingGames",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovieRatingGuesses_AspNetUsers_UserId",
                table: "MovieRatingGuesses");

            migrationBuilder.DropForeignKey(
                name: "FK_MovieRatingGuesses_Movies_MovieId",
                table: "MovieRatingGuesses");

            migrationBuilder.DropForeignKey(
                name: "FK_MovieRatingGuesses_RatingGuessingGames_GuessRatingGameId",
                table: "MovieRatingGuesses");

            migrationBuilder.DropForeignKey(
                name: "FK_RatingGuessingGames_AspNetUsers_UserId",
                table: "RatingGuessingGames");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MovieRatingGuesses",
                table: "MovieRatingGuesses");

            migrationBuilder.DropIndex(
                name: "IX_MovieRatingGuesses_GuessRatingGameId",
                table: "MovieRatingGuesses");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "RatingGuessingGames");

            migrationBuilder.DropColumn(
                name: "GuessRatingGameId",
                table: "MovieRatingGuesses");

            migrationBuilder.RenameTable(
                name: "MovieRatingGuesses",
                newName: "MovieRatingGuess");

            migrationBuilder.RenameIndex(
                name: "IX_MovieRatingGuesses_UserId",
                table: "MovieRatingGuess",
                newName: "IX_MovieRatingGuess_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_MovieRatingGuesses_MovieId",
                table: "MovieRatingGuess",
                newName: "IX_MovieRatingGuess_MovieId");

            migrationBuilder.AlterColumn<int>(
                name: "RatingGuessValue",
                table: "MovieRatingGuess",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AddColumn<int>(
                name: "MovieId1",
                table: "MovieRatingGuess",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_MovieRatingGuess",
                table: "MovieRatingGuess",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_MovieRatingGuess_MovieId1",
                table: "MovieRatingGuess",
                column: "MovieId1");

            migrationBuilder.AddForeignKey(
                name: "FK_MovieRatingGuess_AspNetUsers_UserId",
                table: "MovieRatingGuess",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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

            migrationBuilder.AddForeignKey(
                name: "FK_RatingGuessingGames_AspNetUsers_UserId",
                table: "RatingGuessingGames",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
