using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Server.Implementations.Migrations
{
    /// <summary>
    /// Stage 3: Create unique index after removing duplicate People records.
    /// </summary>
    public partial class RemoveDuplicatePeopleStage3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Peoples_Name_PersonType_Temp",
                table: "Peoples");

            migrationBuilder.AlterColumn<string>(
                name: "PersonType",
                table: "Peoples",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Peoples_Name_PersonType",
                table: "Peoples",
                columns: new[] { "Name", "PersonType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Peoples_Name_PersonType",
                table: "Peoples");

            migrationBuilder.AlterColumn<string>(
                name: "PersonType",
                table: "Peoples",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.CreateIndex(
                name: "IX_Peoples_Name_PersonType_Temp",
                table: "Peoples",
                columns: new[] { "Name", "PersonType" });
        }
    }
}
