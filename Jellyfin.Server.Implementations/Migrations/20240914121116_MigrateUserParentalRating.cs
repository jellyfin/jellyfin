using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Server.Implementations.Migrations
{
    /// <inheritdoc />
    public partial class MigrateUserParentalRating : Migration
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

            migrationBuilder.AlterColumn<string>(
                name: "SegmentProviderId",
                table: "MediaSegments",
                type: "TEXT",
                nullable: false,
                defaultValue: string.Empty,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxParentalRatingSubScore",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "MaxParentalRatingScore",
                table: "Users",
                newName: "MaxParentalAgeRating");

            migrationBuilder.AlterColumn<string>(
                name: "SegmentProviderId",
                table: "MediaSegments",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");
        }
    }
}
