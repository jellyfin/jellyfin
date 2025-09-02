using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Server.Implementations.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthenticationProviderData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuthenticationProviderData",
                columns: table => new
                {
                    AuthenticationProviderId = table.Column<string>(type: "TEXT", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Data = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthenticationProviderData", x => x.AuthenticationProviderId);
                });

            migrationBuilder.CreateTable(
                name: "UserAuthenticationProviderData",
                columns: table => new
                {
                    AuthenticationProviderId = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Data = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAuthenticationProviderData", x => new { x.AuthenticationProviderId, x.UserId });
                    table.ForeignKey(
                        name: "FK_UserAuthenticationProviderData_AuthenticationProviderData_AuthenticationProviderId",
                        column: x => x.AuthenticationProviderId,
                        principalTable: "AuthenticationProviderData",
                        principalColumn: "AuthenticationProviderId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserAuthenticationProviderData_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserAuthenticationProviderData_UserId",
                table: "UserAuthenticationProviderData",
                column: "UserId");

            migrationBuilder.Sql("""
INSERT INTO AuthenticationProviderData (AuthenticationProviderId, IsEnabled) VALUES ("Jellyfin.Server.Implementations.Users.DefaultAuthenticationProvider", 1);
""");

            migrationBuilder.Sql("""
INSERT INTO UserAuthenticationProviderData (UserId, Data, AuthenticationProviderId) SELECT Id, '{"PasswordHash":"' || Password || '","MfaSetup":false,"TOTPSecret":null}', 'Jellyfin.Server.Implementations.Users.DefaultAuthenticationProvider' FROM Users;
""");

            migrationBuilder.DropColumn(
                name: "AuthenticationProviderId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MustUpdatePassword",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Password",
                table: "Users");
        }

        // migrating down not supported here
        /* /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthenticationProviderId",
                table: "Users",
                type: "TEXT",
                maxLength: 255,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<bool>(
                name: "MustUpdatePassword",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Password",
                table: "Users",
                type: "TEXT",
                maxLength: 65535,
                nullable: true);

            // TODO: migration logic to extract password from Jellyfin.Server.Implementations.Users.DefaultAuthenticationProvider provider and insert it into users table

            migrationBuilder.DropTable(
                name: "UserAuthenticationProviderData");

            migrationBuilder.DropTable(
                name: "AuthenticationProviderData");
        }*/
    }
}
