using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Server.Implementations.Migrations
{
    /// <inheritdoc />
    public partial class AddInheritedParentalRatingSubValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MaxParentalAgeRating",
                table: "Users",
                newName: "MaxParentalRatingScore");

            migrationBuilder.AddColumn<int>(
                name: "MaxParentalRatingSubScore",
                table: "Users",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InheritedParentalRatingSubValue",
                table: "BaseItems",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxParentalRatingSubScore",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "InheritedParentalRatingValue",
                table: "BaseItems");

            migrationBuilder.RenameColumn(
                name: "MaxParentalRatingScore",
                table: "Users",
                newName: "MaxParentalAgeRating");
        }
    }
}
