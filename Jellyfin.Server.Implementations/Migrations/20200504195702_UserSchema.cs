#pragma warning disable CS1591
#pragma warning disable SA1601

using Microsoft.EntityFrameworkCore.Migrations;

namespace Jellyfin.Server.Implementations.Migrations
{
    public partial class UserSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "User",
                schema: "jellyfin",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(maxLength: 255, nullable: false),
                    Password = table.Column<string>(maxLength: 65535, nullable: true),
                    MustUpdatePassword = table.Column<bool>(nullable: false),
                    AudioLanguagePreference = table.Column<string>(maxLength: 255, nullable: false),
                    AuthenticationProviderId = table.Column<string>(maxLength: 255, nullable: false),
                    GroupedFolders = table.Column<string>(maxLength: 65535, nullable: true),
                    InvalidLoginAttemptCount = table.Column<int>(nullable: false),
                    LatestItemExcludes = table.Column<string>(maxLength: 65535, nullable: true),
                    LoginAttemptsBeforeLockout = table.Column<int>(nullable: true),
                    MyMediaExcludes = table.Column<string>(maxLength: 65535, nullable: true),
                    OrderedViews = table.Column<string>(maxLength: 65535, nullable: true),
                    SubtitleMode = table.Column<string>(maxLength: 255, nullable: false),
                    PlayDefaultAudioTrack = table.Column<bool>(nullable: false),
                    SubtitleLanguagePrefernce = table.Column<string>(maxLength: 255, nullable: true),
                    DisplayMissingEpisodes = table.Column<bool>(nullable: true),
                    DisplayCollectionsView = table.Column<bool>(nullable: true),
                    HidePlayedInLatest = table.Column<bool>(nullable: true),
                    RememberAudioSelections = table.Column<bool>(nullable: true),
                    RememberSubtitleSelections = table.Column<bool>(nullable: true),
                    EnableNextEpisodeAutoPlay = table.Column<bool>(nullable: true),
                    EnableUserPreferenceAccess = table.Column<bool>(nullable: true),
                    RowVersion = table.Column<uint>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Group",
                schema: "jellyfin",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(maxLength: 255, nullable: false),
                    RowVersion = table.Column<uint>(nullable: false),
                    Group_Groups_Id = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Group", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Group_User_Group_Groups_Id",
                        column: x => x.Group_Groups_Id,
                        principalSchema: "jellyfin",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Permission",
                schema: "jellyfin",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Kind = table.Column<int>(nullable: false),
                    Value = table.Column<bool>(nullable: false),
                    RowVersion = table.Column<uint>(nullable: false),
                    Permission_GroupPermissions_Id = table.Column<int>(nullable: true),
                    Permission_Permissions_Id = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permission", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Permission_Group_Permission_GroupPermissions_Id",
                        column: x => x.Permission_GroupPermissions_Id,
                        principalSchema: "jellyfin",
                        principalTable: "Group",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Permission_User_Permission_Permissions_Id",
                        column: x => x.Permission_Permissions_Id,
                        principalSchema: "jellyfin",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Preference",
                schema: "jellyfin",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Kind = table.Column<int>(nullable: false),
                    Value = table.Column<string>(maxLength: 65535, nullable: false),
                    RowVersion = table.Column<uint>(nullable: false),
                    Preference_Preferences_Id = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Preference", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Preference_Group_Preference_Preferences_Id",
                        column: x => x.Preference_Preferences_Id,
                        principalSchema: "jellyfin",
                        principalTable: "Group",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Preference_User_Preference_Preferences_Id",
                        column: x => x.Preference_Preferences_Id,
                        principalSchema: "jellyfin",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProviderMapping",
                schema: "jellyfin",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProviderName = table.Column<string>(maxLength: 255, nullable: false),
                    ProviderSecrets = table.Column<string>(maxLength: 65535, nullable: false),
                    ProviderData = table.Column<string>(maxLength: 65535, nullable: false),
                    RowVersion = table.Column<uint>(nullable: false),
                    ProviderMapping_ProviderMappings_Id = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderMapping", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProviderMapping_Group_ProviderMapping_ProviderMappings_Id",
                        column: x => x.ProviderMapping_ProviderMappings_Id,
                        principalSchema: "jellyfin",
                        principalTable: "Group",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProviderMapping_User_ProviderMapping_ProviderMappings_Id",
                        column: x => x.ProviderMapping_ProviderMappings_Id,
                        principalSchema: "jellyfin",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Group_Group_Groups_Id",
                schema: "jellyfin",
                table: "Group",
                column: "Group_Groups_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Permission_Permission_GroupPermissions_Id",
                schema: "jellyfin",
                table: "Permission",
                column: "Permission_GroupPermissions_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Permission_Permission_Permissions_Id",
                schema: "jellyfin",
                table: "Permission",
                column: "Permission_Permissions_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Preference_Preference_Preferences_Id",
                schema: "jellyfin",
                table: "Preference",
                column: "Preference_Preferences_Id");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderMapping_ProviderMapping_ProviderMappings_Id",
                schema: "jellyfin",
                table: "ProviderMapping",
                column: "ProviderMapping_ProviderMappings_Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Permission",
                schema: "jellyfin");

            migrationBuilder.DropTable(
                name: "Preference",
                schema: "jellyfin");

            migrationBuilder.DropTable(
                name: "ProviderMapping",
                schema: "jellyfin");

            migrationBuilder.DropTable(
                name: "Group",
                schema: "jellyfin");

            migrationBuilder.DropTable(
                name: "User",
                schema: "jellyfin");
        }
    }
}
