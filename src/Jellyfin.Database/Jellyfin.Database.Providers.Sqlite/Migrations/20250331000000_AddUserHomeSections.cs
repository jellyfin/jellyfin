using Microsoft.EntityFrameworkCore.Migrations;

namespace Jellyfin.Database.Providers.Sqlite.Migrations
{
    /// <summary>
    /// Migration to add UserHomeSections table.
    /// </summary>
    public partial class AddUserHomeSections : Migration
    {
        /// <summary>
        /// Builds the operations that will migrate the database 'up'.
        /// </summary>
        /// <param name="migrationBuilder">The migration builder.</param>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserHomeSections",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(nullable: false),
                    SectionId = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    SectionType = table.Column<int>(nullable: false),
                    Priority = table.Column<int>(nullable: false),
                    MaxItems = table.Column<int>(nullable: false),
                    SortOrder = table.Column<int>(nullable: false),
                    SortBy = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserHomeSections", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserHomeSections_UserId_SectionId",
                table: "UserHomeSections",
                columns: new[] { "UserId", "SectionId" },
                unique: true);
        }

        /// <summary>
        /// Builds the operations that will migrate the database 'down'.
        /// </summary>
        /// <param name="migrationBuilder">The migration builder.</param>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserHomeSections");
        }
    }
}
