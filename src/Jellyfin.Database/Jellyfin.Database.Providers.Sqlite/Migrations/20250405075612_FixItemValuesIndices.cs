using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Server.Implementations.Migrations
{
    /// <inheritdoc />
    public partial class FixItemValuesIndices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ItemValues_Type_CleanValue",
                table: "ItemValues");

            migrationBuilder.CreateIndex(
                name: "IX_ItemValues_Type_CleanValue",
                table: "ItemValues",
                columns: new[] { "Type", "CleanValue" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemValues_Type_Value",
                table: "ItemValues",
                columns: new[] { "Type", "Value" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ItemValues_Type_CleanValue",
                table: "ItemValues");

            migrationBuilder.DropIndex(
                name: "IX_ItemValues_Type_Value",
                table: "ItemValues");

            migrationBuilder.CreateIndex(
                name: "IX_ItemValues_Type_CleanValue",
                table: "ItemValues",
                columns: new[] { "Type", "CleanValue" },
                unique: true);
        }
    }
}
