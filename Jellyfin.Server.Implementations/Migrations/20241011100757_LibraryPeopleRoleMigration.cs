using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Server.Implementations.Migrations
{
    /// <inheritdoc />
    public partial class LibraryPeopleRoleMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                table: "Peoples");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "PeopleBaseItemMap",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                table: "PeopleBaseItemMap");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Peoples",
                type: "TEXT",
                nullable: true);
        }
    }
}
