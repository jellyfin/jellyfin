#pragma warning disable CS1591
#pragma warning disable SA1601

using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Jellyfin.Server.Implementations.Migrations
{
    public partial class AddUsers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImageInfo",
                schema: "jellyfin",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Path = table.Column<string>(maxLength: 512, nullable: false),
                    LastModified = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageInfo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "jellyfin",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Username = table.Column<string>(maxLength: 255, nullable: false),
                    Password = table.Column<string>(maxLength: 65535, nullable: true),
                    EasyPassword = table.Column<string>(maxLength: 65535, nullable: true),
                    MustUpdatePassword = table.Column<bool>(nullable: false),
                    AudioLanguagePreference = table.Column<string>(maxLength: 255, nullable: true),
                    AuthenticationProviderId = table.Column<string>(maxLength: 255, nullable: false),
                    PasswordResetProviderId = table.Column<string>(maxLength: 255, nullable: false),
                    InvalidLoginAttemptCount = table.Column<int>(nullable: false),
                    LastActivityDate = table.Column<DateTime>(nullable: true),
                    LastLoginDate = table.Column<DateTime>(nullable: true),
                    LoginAttemptsBeforeLockout = table.Column<int>(nullable: true),
                    SubtitleMode = table.Column<int>(nullable: false),
                    PlayDefaultAudioTrack = table.Column<bool>(nullable: false),
                    SubtitleLanguagePreference = table.Column<string>(maxLength: 255, nullable: true),
                    DisplayMissingEpisodes = table.Column<bool>(nullable: false),
                    DisplayCollectionsView = table.Column<bool>(nullable: false),
                    EnableLocalPassword = table.Column<bool>(nullable: false),
                    HidePlayedInLatest = table.Column<bool>(nullable: false),
                    RememberAudioSelections = table.Column<bool>(nullable: false),
                    RememberSubtitleSelections = table.Column<bool>(nullable: false),
                    EnableNextEpisodeAutoPlay = table.Column<bool>(nullable: false),
                    EnableAutoLogin = table.Column<bool>(nullable: false),
                    EnableUserPreferenceAccess = table.Column<bool>(nullable: false),
                    MaxParentalAgeRating = table.Column<int>(nullable: true),
                    RemoteClientBitrateLimit = table.Column<int>(nullable: true),
                    InternalId = table.Column<long>(nullable: false),
                    ProfileImageId = table.Column<int>(nullable: true),
                    SyncPlayAccess = table.Column<int>(nullable: false),
                    RowVersion = table.Column<uint>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_ImageInfo_ProfileImageId",
                        column: x => x.ProfileImageId,
                        principalSchema: "jellyfin",
                        principalTable: "ImageInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccessSchedule",
                schema: "jellyfin",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<Guid>(nullable: false),
                    DayOfWeek = table.Column<int>(nullable: false),
                    StartHour = table.Column<double>(nullable: false),
                    EndHour = table.Column<double>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessSchedule", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccessSchedule_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "jellyfin",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                schema: "jellyfin",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Kind = table.Column<int>(nullable: false),
                    Value = table.Column<bool>(nullable: false),
                    RowVersion = table.Column<uint>(nullable: false),
                    Permission_Permissions_Guid = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Permissions_Users_Permission_Permissions_Guid",
                        column: x => x.Permission_Permissions_Guid,
                        principalSchema: "jellyfin",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Preferences",
                schema: "jellyfin",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Kind = table.Column<int>(nullable: false),
                    Value = table.Column<string>(maxLength: 65535, nullable: false),
                    RowVersion = table.Column<uint>(nullable: false),
                    Preference_Preferences_Guid = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Preferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Preferences_Users_Preference_Preferences_Guid",
                        column: x => x.Preference_Preferences_Guid,
                        principalSchema: "jellyfin",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccessSchedule_UserId",
                schema: "jellyfin",
                table: "AccessSchedule",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Permission_Permissions_Guid",
                schema: "jellyfin",
                table: "Permissions",
                column: "Permission_Permissions_Guid");

            migrationBuilder.CreateIndex(
                name: "IX_Preferences_Preference_Preferences_Guid",
                schema: "jellyfin",
                table: "Preferences",
                column: "Preference_Preferences_Guid");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ProfileImageId",
                schema: "jellyfin",
                table: "Users",
                column: "ProfileImageId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessSchedule",
                schema: "jellyfin");

            migrationBuilder.DropTable(
                name: "Permissions",
                schema: "jellyfin");

            migrationBuilder.DropTable(
                name: "Preferences",
                schema: "jellyfin");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "jellyfin");

            migrationBuilder.DropTable(
                name: "ImageInfo",
                schema: "jellyfin");
        }
    }
}
