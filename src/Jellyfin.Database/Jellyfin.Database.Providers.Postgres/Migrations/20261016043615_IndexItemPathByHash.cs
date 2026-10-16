using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Jellyfin.Database.Providers.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class IndexItemPathByHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BaseItems_Path",
                table: "BaseItems");

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Path",
                table: "BaseItems",
                column: "Path")
                .Annotation("Npgsql:IndexMethod", "hash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BaseItems_Path",
                table: "BaseItems");

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Path",
                table: "BaseItems",
                column: "Path")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsFirst });
        }
    }
}
