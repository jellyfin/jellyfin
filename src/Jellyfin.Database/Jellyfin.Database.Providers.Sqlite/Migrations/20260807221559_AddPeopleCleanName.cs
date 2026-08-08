using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Database.Providers.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddPeopleCleanName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CleanName",
                table: "Peoples",
                type: "TEXT",
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.CreateIndex(
                name: "IX_Peoples_CleanName_PersonType",
                table: "Peoples",
                columns: new[] { "CleanName", "PersonType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Peoples_CleanName_PersonType",
                table: "Peoples");

            migrationBuilder.DropColumn(
                name: "CleanName",
                table: "Peoples");
        }
    }
}
