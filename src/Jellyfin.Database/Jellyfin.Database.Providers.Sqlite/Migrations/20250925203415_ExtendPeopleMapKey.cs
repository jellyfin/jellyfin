using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Server.Implementations.Migrations
{
    /// <inheritdoc />
    public partial class ExtendPeopleMapKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_PeopleBaseItemMap",
                table: "PeopleBaseItemMap");

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "PeopleBaseItemMap",
                type: "TEXT",
                nullable: false,
                defaultValue: string.Empty,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PeopleBaseItemMap",
                table: "PeopleBaseItemMap",
                columns: new[] { "ItemId", "PeopleId", "Role" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_PeopleBaseItemMap",
                table: "PeopleBaseItemMap");

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "PeopleBaseItemMap",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PeopleBaseItemMap",
                table: "PeopleBaseItemMap",
                columns: new[] { "ItemId", "PeopleId" });
        }
    }
}
