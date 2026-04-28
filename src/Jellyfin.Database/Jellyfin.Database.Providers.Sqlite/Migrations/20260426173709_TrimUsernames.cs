using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Server.Implementations.Migrations
{
    /// <inheritdoc />
    public partial class TrimUsernames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                WITH ranked AS (
                    SELECT "Id", "Username",
                           ROW_NUMBER() OVER (PARTITION BY TRIM("Username") ORDER BY LENGTH("Username")) as rn
                    FROM "Users"
                )
                UPDATE "Users"
                SET "Username" = TRIM("Username") || ' ' || ranked.rn
                FROM ranked
                WHERE "Users"."Id" = ranked."Id" AND ranked.rn > 1;
                """);

            migrationBuilder.Sql(
                """
                UPDATE "Users" SET "Username" = TRIM("Username")
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException(
                "This migration cannot be reversed because trimmed whitespace cannot be restored.");
        }
    }
}
