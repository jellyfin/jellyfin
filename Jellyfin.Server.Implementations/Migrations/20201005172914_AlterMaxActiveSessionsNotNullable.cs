#pragma warning disable CS1591
#pragma warning disable SA1601

using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Jellyfin.Server.Implementations.Migrations
{
    public partial class AlterMaxActiveSessionsNotNullable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Users SET MaxActiveSessions = 0 WHERE MaxActiveSessions IS NULL");
            migrationBuilder.CreateTable(
                name: "Users_new",
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
                    SyncPlayAccess = table.Column<int>(nullable: false),
                    RowVersion = table.Column<uint>(nullable: false),
                    MaxActiveSessions = table.Column<int>(nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.Sql("INSERT INTO Users_new SELECT * FROM Users;");
            migrationBuilder.Sql("PRAGMA foreign_keys=\"0\"", true);
            migrationBuilder.Sql("DROP TABLE Users", true);
            migrationBuilder.Sql("ALTER TABLE Users_new RENAME TO Users", true);
            migrationBuilder.Sql("PRAGMA foreign_keys=\"1\"", true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users_new",
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
                    SyncPlayAccess = table.Column<int>(nullable: false),
                    RowVersion = table.Column<uint>(nullable: false),
                    MaxActiveSessions = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.Sql("INSERT INTO Users_new SELECT * FROM Users;");
            migrationBuilder.Sql("PRAGMA foreign_keys=\"0\"", true);
            migrationBuilder.Sql("DROP TABLE Users", true);
            migrationBuilder.Sql("ALTER TABLE Users_new RENAME TO Users", true);
            migrationBuilder.Sql("UPDATE Users SET MaxActiveSessions = NULL WHERE MaxActiveSessions = 0");
            migrationBuilder.Sql("PRAGMA foreign_keys=\"1\"", true);
        }
    }
}
