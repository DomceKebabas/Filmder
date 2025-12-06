using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Filmder.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupPicture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GroupPictureUrl",
                table: "Groups",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GroupPictureUrl",
                table: "Groups");
        }
    }
}
