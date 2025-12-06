using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Filmder.Migrations
{
    /// <inheritdoc />
    public partial class RatingGameEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "RatingGuessingGames",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "TotalMovies",
                table: "RatingGuessingGames",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "RatingGuessingGames");

            migrationBuilder.DropColumn(
                name: "TotalMovies",
                table: "RatingGuessingGames");
        }
    }
}
