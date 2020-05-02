#pragma warning disable CS1591
#pragma warning disable SA1601

using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Jellyfin.Server.Implementations.Migrations
{
    public partial class InitialSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "jellyfin");

            migrationBuilder.CreateTable(
                name: "ActivityLog",
                schema: "jellyfin",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(maxLength: 512, nullable: false),
                    Overview = table.Column<string>(maxLength: 512, nullable: true),
                    ShortOverview = table.Column<string>(maxLength: 512, nullable: true),
                    Type = table.Column<string>(maxLength: 256, nullable: false),
                    UserId = table.Column<Guid>(nullable: false),
                    ItemId = table.Column<string>(maxLength: 256, nullable: true),
                    DateCreated = table.Column<DateTime>(nullable: false),
                    LogSeverity = table.Column<int>(nullable: false),
                    RowVersion = table.Column<uint>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Collection",
                schema: "jellyfin",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(maxLength: 1024, nullable: true),
                    RowVersion = table.Column<uint>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Collection", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Library",
                schema: "jellyfin",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(maxLength: 1024, nullable: false),
                    RowVersion = table.Column<uint>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Library", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MetadataProvider",
                schema: "jellyfin",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(maxLength: 1024, nullable: false),
                    RowVersion = table.Column<uint>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetadataProvider", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Person",
                schema: "jellyfin",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UrlId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 1024, nullable: false),
                    SourceId = table.Column<string>(maxLength: 255, nullable: true),
                    DateAdded = table.Column<DateTime>(nullable: false),
                    DateModified = table.Column<DateTime>(nullable: false),
                    RowVersion = table.Column<uint>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Person", x => x.Id);
                });

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
                name: "LibraryRoot",
                schema: "jellyfin",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Path = table.Column<string>(maxLength: 65535, nullable: false),
                    NetworkPath = table.Column<string>(maxLength: 65535, nullable: true),
                    RowVersion = table.Column<uint>(nullable: false),
                    Library_Id = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryRoot", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LibraryRoot_Library_Library_Id",
                        column: x => x.Library_Id,
                        principalSchema: "jellyfin",
                        principalTable: "Library",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                name: "LibraryItem",
                schema: "jellyfin",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UrlId = table.Column<Guid>(nullable: false),
                    DateAdded = table.Column<DateTime>(nullable: false),
                    RowVersion = table.Column<uint>(nullable: false),
                    LibraryRoot_Id = table.Column<int>(nullable: true),
                    Discriminator = table.Column<string>(nullable: false),
                    EpisodeNumber = table.Column<int>(nullable: true),
                    Episode_Episodes_Id = table.Column<int>(nullable: true),
                    SeasonNumber = table.Column<int>(nullable: true),
                    Season_Seasons_Id = table.Column<int>(nullable: true),
                    AirsDayOfWeek = table.Column<int>(nullable: true),
                    AirsTime = table.Column<DateTimeOffset>(nullable: true),
                    FirstAired = table.Column<DateTimeOffset>(nullable: true),
                    TrackNumber = table.Column<int>(nullable: true),
                    Track_Tracks_Id = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LibraryItem_LibraryItem_Episode_Episodes_Id",
                        column: x => x.Episode_Episodes_Id,
                        principalSchema: "jellyfin",
                        principalTable: "LibraryItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LibraryItem_LibraryRoot_LibraryRoot_Id",
                        column: x => x.LibraryRoot_Id,
                        principalSchema: "jellyfin",
                        principalTable: "LibraryRoot",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LibraryItem_LibraryItem_Season_Seasons_Id",
                        column: x => x.Season_Seasons_Id,
                        principalSchema: "jellyfin",
                        principalTable: "LibraryItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LibraryItem_LibraryItem_Track_Tracks_Id",
                        column: x => x.Track_Tracks_Id,
                        principalSchema: "jellyfin",
                        principalTable: "LibraryItem",
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

            migrationBuilder.CreateTable(
                name: "CollectionItem",
                schema: "jellyfin",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RowVersion = table.Column<uint>(nullable: false),
                    LibraryItem_Id = table.Column<int>(nullable: true),
                    CollectionItem_Next_Id = table.Column<int>(nullable: true),
                    CollectionItem_Previous_Id = table.Column<int>(nullable: true),
                    CollectionItem_CollectionItem_Id = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CollectionItem_Collection_CollectionItem_CollectionItem_Id",
                        column: x => x.CollectionItem_CollectionItem_Id,
                        principalSchema: "jellyfin",
                        principalTable: "Collection",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CollectionItem_CollectionItem_CollectionItem_Next_Id",
                        column: x => x.CollectionItem_Next_Id,
                        principalSchema: "jellyfin",
                        principalTable: "CollectionItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CollectionItem_CollectionItem_CollectionItem_Previous_Id",
                        column: x => x.CollectionItem_Previous_Id,
                        principalSchema: "jellyfin",
                        principalTable: "CollectionItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CollectionItem_LibraryItem_LibraryItem_Id",
                        column: x => x.LibraryItem_Id,
                        principalSchema: "jellyfin",
                        principalTable: "LibraryItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Release",
                schema: "jellyfin",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(maxLength: 1024, nullable: false),
                    RowVersion = table.Column<uint>(nullable: false),
                    Release_Releases_Id = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Release", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Release_LibraryItem_Release_Releases_Id",
                        column: x => x.Release_Releases_Id,
                        principalSchema: "jellyfin",
                        principalTable: "LibraryItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Release_LibraryItem_Release_Releases_Id1",
                        column: x => x.Release_Releases_Id,
                        principalSchema: "jellyfin",
                        principalTable: "LibraryItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Release_LibraryItem_Release_Releases_Id2",
                        column: x => x.Release_Releases_Id,
                        principalSchema: "jellyfin",
                        principalTable: "LibraryItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Release_LibraryItem_Release_Releases_Id3",
                        column: x => x.Release_Releases_Id,
                        principalSchema: "jellyfin",
                        principalTable: "LibraryItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Release_LibraryItem_Release_Releases_Id4",
                        column: x => x.Release_Releases_Id,
                        principalSchema: "jellyfin",
                        principalTable: "LibraryItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Release_LibraryItem_Release_Releases_Id5",
                        column: x => x.Release_Releases_Id,
                        principalSchema: "jellyfin",
                        principalTable: "LibraryItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Chapter",
                schema: "jellyfin",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(maxLength: 1024, nullable: true),
                    Language = table.Column<string>(maxLength: 3, nullable: false),
                    TimeStart = table.Column<long>(nullable: false),
                    TimeEnd = table.Column<long>(nullable: true),
                    RowVersion = table.Column<uint>(nullable: false),
                    Chapter_Chapters_Id = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chapter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Chapter_Release_Chapter_Chapters_Id",
                        column: x => x.Chapter_Chapters_Id,
                        principalSchema: "jellyfin",
                        principalTable: "Release",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MediaFile",
                schema: "jellyfin",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Path = table.Column<string>(maxLength: 65535, nullable: false),
                    Kind = table.Column<int>(nullable: false),
                    RowVersion = table.Column<uint>(nullable: false),
                    MediaFile_MediaFiles_Id = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaFile", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaFile_Release_MediaFile_MediaFiles_Id",
                        column: x => x.MediaFile_MediaFiles_Id,
                        principalSchema: "jellyfin",
                        principalTable: "Release",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MediaFileStream",
                schema: "jellyfin",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StreamNumber = table.Column<int>(nullable: false),
                    RowVersion = table.Column<uint>(nullable: false),
                    MediaFileStream_MediaFileStreams_Id = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaFileStream", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaFileStream_MediaFile_MediaFileStream_MediaFileStreams_Id",
                        column: x => x.MediaFileStream_MediaFileStreams_Id,
                        principalSchema: "jellyfin",
                        principalTable: "MediaFile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PersonRole",
                schema: "jellyfin",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Role = table.Column<string>(maxLength: 1024, nullable: true),
                    Type = table.Column<int>(nullable: false),
                    RowVersion = table.Column<uint>(nullable: false),
                    Person_Id = table.Column<int>(nullable: true),
                    Artwork_Artwork_Id = table.Column<int>(nullable: true),
                    PersonRole_PersonRoles_Id = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonRole", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonRole_Person_Person_Id",
                        column: x => x.Person_Id,
                        principalSchema: "jellyfin",
                        principalTable: "Person",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Metadata",
                schema: "jellyfin",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(maxLength: 1024, nullable: false),
                    OriginalTitle = table.Column<string>(maxLength: 1024, nullable: true),
                    SortTitle = table.Column<string>(maxLength: 1024, nullable: true),
                    Language = table.Column<string>(maxLength: 3, nullable: false),
                    ReleaseDate = table.Column<DateTimeOffset>(nullable: true),
                    DateAdded = table.Column<DateTime>(nullable: false),
                    DateModified = table.Column<DateTime>(nullable: false),
                    RowVersion = table.Column<uint>(nullable: false),
                    Discriminator = table.Column<string>(nullable: false),
                    ISBN = table.Column<long>(nullable: true),
                    BookMetadata_BookMetadata_Id = table.Column<int>(nullable: true),
                    Description = table.Column<string>(maxLength: 65535, nullable: true),
                    Headquarters = table.Column<string>(maxLength: 255, nullable: true),
                    Country = table.Column<string>(maxLength: 2, nullable: true),
                    Homepage = table.Column<string>(maxLength: 1024, nullable: true),
                    CompanyMetadata_CompanyMetadata_Id = table.Column<int>(nullable: true),
                    CustomItemMetadata_CustomItemMetadata_Id = table.Column<int>(nullable: true),
                    Outline = table.Column<string>(maxLength: 1024, nullable: true),
                    Plot = table.Column<string>(maxLength: 65535, nullable: true),
                    Tagline = table.Column<string>(maxLength: 1024, nullable: true),
                    EpisodeMetadata_EpisodeMetadata_Id = table.Column<int>(nullable: true),
                    MovieMetadata_Outline = table.Column<string>(maxLength: 1024, nullable: true),
                    MovieMetadata_Plot = table.Column<string>(maxLength: 65535, nullable: true),
                    MovieMetadata_Tagline = table.Column<string>(maxLength: 1024, nullable: true),
                    MovieMetadata_Country = table.Column<string>(maxLength: 2, nullable: true),
                    MovieMetadata_MovieMetadata_Id = table.Column<int>(nullable: true),
                    Barcode = table.Column<string>(maxLength: 255, nullable: true),
                    LabelNumber = table.Column<string>(maxLength: 255, nullable: true),
                    MusicAlbumMetadata_Country = table.Column<string>(maxLength: 2, nullable: true),
                    MusicAlbumMetadata_MusicAlbumMetadata_Id = table.Column<int>(nullable: true),
                    PhotoMetadata_PhotoMetadata_Id = table.Column<int>(nullable: true),
                    SeasonMetadata_Outline = table.Column<string>(maxLength: 1024, nullable: true),
                    SeasonMetadata_SeasonMetadata_Id = table.Column<int>(nullable: true),
                    SeriesMetadata_Outline = table.Column<string>(maxLength: 1024, nullable: true),
                    SeriesMetadata_Plot = table.Column<string>(maxLength: 65535, nullable: true),
                    SeriesMetadata_Tagline = table.Column<string>(maxLength: 1024, nullable: true),
                    SeriesMetadata_Country = table.Column<string>(maxLength: 2, nullable: true),
                    SeriesMetadata_SeriesMetadata_Id = table.Column<int>(nullable: true),
                    TrackMetadata_TrackMetadata_Id = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Metadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Metadata_LibraryItem_BookMetadata_BookMetadata_Id",
                        column: x => x.BookMetadata_BookMetadata_Id,
                        principalSchema: "jellyfin",
                        principalTable: "LibraryItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Metadata_LibraryItem_CustomItemMetadata_CustomItemMetadata_Id",
                        column: x => x.CustomItemMetadata_CustomItemMetadata_Id,
                        principalSchema: "jellyfin",
                        principalTable: "LibraryItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Metadata_LibraryItem_EpisodeMetadata_EpisodeMetadata_Id",
                        column: x => x.EpisodeMetadata_EpisodeMetadata_Id,
                        principalSchema: "jellyfin",
                        principalTable: "LibraryItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Metadata_LibraryItem_MovieMetadata_MovieMetadata_Id",
                        column: x => x.MovieMetadata_MovieMetadata_Id,
                        principalSchema: "jellyfin",
                        principalTable: "LibraryItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Metadata_LibraryItem_MusicAlbumMetadata_MusicAlbumMetadata_Id",
                        column: x => x.MusicAlbumMetadata_MusicAlbumMetadata_Id,
                        principalSchema: "jellyfin",
                        principalTable: "LibraryItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Metadata_LibraryItem_PhotoMetadata_PhotoMetadata_Id",
                        column: x => x.PhotoMetadata_PhotoMetadata_Id,
                        principalSchema: "jellyfin",
                        principalTable: "LibraryItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Metadata_LibraryItem_SeasonMetadata_SeasonMetadata_Id",
                        column: x => x.SeasonMetadata_SeasonMetadata_Id,
                        principalSchema: "jellyfin",
                        principalTable: "LibraryItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Metadata_LibraryItem_SeriesMetadata_SeriesMetadata_Id",
                        column: x => x.SeriesMetadata_SeriesMetadata_Id,
                        principalSchema: "jellyfin",
                        principalTable: "LibraryItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Metadata_LibraryItem_TrackMetadata_TrackMetadata_Id",
                        column: x => x.TrackMetadata_TrackMetadata_Id,
                        principalSchema: "jellyfin",
                        principalTable: "LibraryItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Artwork",
                schema: "jellyfin",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Path = table.Column<string>(maxLength: 65535, nullable: false),
                    Kind = table.Column<int>(nullable: false),
                    RowVersion = table.Column<uint>(nullable: false),
                    PersonRole_PersonRoles_Id = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Artwork", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Artwork_Metadata_PersonRole_PersonRoles_Id",
                        column: x => x.PersonRole_PersonRoles_Id,
                        principalSchema: "jellyfin",
                        principalTable: "Metadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Company",
                schema: "jellyfin",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RowVersion = table.Column<uint>(nullable: false),
                    Company_Parent_Id = table.Column<int>(nullable: true),
                    Company_Labels_Id = table.Column<int>(nullable: true),
                    Company_Networks_Id = table.Column<int>(nullable: true),
                    Company_Publishers_Id = table.Column<int>(nullable: true),
                    Company_Studios_Id = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Company", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Company_Metadata_Company_Labels_Id",
                        column: x => x.Company_Labels_Id,
                        principalSchema: "jellyfin",
                        principalTable: "Metadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Company_Metadata_Company_Networks_Id",
                        column: x => x.Company_Networks_Id,
                        principalSchema: "jellyfin",
                        principalTable: "Metadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Company_Company_Company_Parent_Id",
                        column: x => x.Company_Parent_Id,
                        principalSchema: "jellyfin",
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Company_Metadata_Company_Publishers_Id",
                        column: x => x.Company_Publishers_Id,
                        principalSchema: "jellyfin",
                        principalTable: "Metadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Company_Metadata_Company_Studios_Id",
                        column: x => x.Company_Studios_Id,
                        principalSchema: "jellyfin",
                        principalTable: "Metadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Genre",
                schema: "jellyfin",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(maxLength: 255, nullable: false),
                    RowVersion = table.Column<uint>(nullable: false),
                    PersonRole_PersonRoles_Id = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Genre", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Genre_Metadata_PersonRole_PersonRoles_Id",
                        column: x => x.PersonRole_PersonRoles_Id,
                        principalSchema: "jellyfin",
                        principalTable: "Metadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MetadataProviderId",
                schema: "jellyfin",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProviderId = table.Column<string>(maxLength: 255, nullable: false),
                    RowVersion = table.Column<uint>(nullable: false),
                    MetadataProvider_Id = table.Column<int>(nullable: true),
                    MetadataProviderId_Sources_Id = table.Column<int>(nullable: true),
                    PersonRole_PersonRoles_Id = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetadataProviderId", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MetadataProviderId_Person_MetadataProviderId_Sources_Id",
                        column: x => x.MetadataProviderId_Sources_Id,
                        principalSchema: "jellyfin",
                        principalTable: "Person",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MetadataProviderId_PersonRole_MetadataProviderId_Sources_Id",
                        column: x => x.MetadataProviderId_Sources_Id,
                        principalSchema: "jellyfin",
                        principalTable: "PersonRole",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MetadataProviderId_MetadataProvider_MetadataProvider_Id",
                        column: x => x.MetadataProvider_Id,
                        principalSchema: "jellyfin",
                        principalTable: "MetadataProvider",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MetadataProviderId_Metadata_PersonRole_PersonRoles_Id",
                        column: x => x.PersonRole_PersonRoles_Id,
                        principalSchema: "jellyfin",
                        principalTable: "Metadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RatingSource",
                schema: "jellyfin",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(maxLength: 1024, nullable: true),
                    MaximumValue = table.Column<double>(nullable: false),
                    MinimumValue = table.Column<double>(nullable: false),
                    RowVersion = table.Column<uint>(nullable: false),
                    MetadataProviderId_Source_Id = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RatingSource", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RatingSource_MetadataProviderId_MetadataProviderId_Source_Id",
                        column: x => x.MetadataProviderId_Source_Id,
                        principalSchema: "jellyfin",
                        principalTable: "MetadataProviderId",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Rating",
                schema: "jellyfin",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Value = table.Column<double>(nullable: false),
                    Votes = table.Column<int>(nullable: true),
                    RowVersion = table.Column<uint>(nullable: false),
                    RatingSource_RatingType_Id = table.Column<int>(nullable: true),
                    PersonRole_PersonRoles_Id = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rating", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rating_Metadata_PersonRole_PersonRoles_Id",
                        column: x => x.PersonRole_PersonRoles_Id,
                        principalSchema: "jellyfin",
                        principalTable: "Metadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Rating_RatingSource_RatingSource_RatingType_Id",
                        column: x => x.RatingSource_RatingType_Id,
                        principalSchema: "jellyfin",
                        principalTable: "RatingSource",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Artwork_Kind",
                schema: "jellyfin",
                table: "Artwork",
                column: "Kind");

            migrationBuilder.CreateIndex(
                name: "IX_Artwork_PersonRole_PersonRoles_Id",
                schema: "jellyfin",
                table: "Artwork",
                column: "PersonRole_PersonRoles_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Chapter_Chapter_Chapters_Id",
                schema: "jellyfin",
                table: "Chapter",
                column: "Chapter_Chapters_Id");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionItem_CollectionItem_CollectionItem_Id",
                schema: "jellyfin",
                table: "CollectionItem",
                column: "CollectionItem_CollectionItem_Id");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionItem_CollectionItem_Next_Id",
                schema: "jellyfin",
                table: "CollectionItem",
                column: "CollectionItem_Next_Id");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionItem_CollectionItem_Previous_Id",
                schema: "jellyfin",
                table: "CollectionItem",
                column: "CollectionItem_Previous_Id");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionItem_LibraryItem_Id",
                schema: "jellyfin",
                table: "CollectionItem",
                column: "LibraryItem_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Company_Company_Labels_Id",
                schema: "jellyfin",
                table: "Company",
                column: "Company_Labels_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Company_Company_Networks_Id",
                schema: "jellyfin",
                table: "Company",
                column: "Company_Networks_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Company_Company_Parent_Id",
                schema: "jellyfin",
                table: "Company",
                column: "Company_Parent_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Company_Company_Publishers_Id",
                schema: "jellyfin",
                table: "Company",
                column: "Company_Publishers_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Company_Company_Studios_Id",
                schema: "jellyfin",
                table: "Company",
                column: "Company_Studios_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Genre_Name",
                schema: "jellyfin",
                table: "Genre",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Genre_PersonRole_PersonRoles_Id",
                schema: "jellyfin",
                table: "Genre",
                column: "PersonRole_PersonRoles_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Group_Group_Groups_Id",
                schema: "jellyfin",
                table: "Group",
                column: "Group_Groups_Id");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryItem_Episode_Episodes_Id",
                schema: "jellyfin",
                table: "LibraryItem",
                column: "Episode_Episodes_Id");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryItem_LibraryRoot_Id",
                schema: "jellyfin",
                table: "LibraryItem",
                column: "LibraryRoot_Id");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryItem_UrlId",
                schema: "jellyfin",
                table: "LibraryItem",
                column: "UrlId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LibraryItem_Season_Seasons_Id",
                schema: "jellyfin",
                table: "LibraryItem",
                column: "Season_Seasons_Id");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryItem_Track_Tracks_Id",
                schema: "jellyfin",
                table: "LibraryItem",
                column: "Track_Tracks_Id");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryRoot_Library_Id",
                schema: "jellyfin",
                table: "LibraryRoot",
                column: "Library_Id");

            migrationBuilder.CreateIndex(
                name: "IX_MediaFile_MediaFile_MediaFiles_Id",
                schema: "jellyfin",
                table: "MediaFile",
                column: "MediaFile_MediaFiles_Id");

            migrationBuilder.CreateIndex(
                name: "IX_MediaFileStream_MediaFileStream_MediaFileStreams_Id",
                schema: "jellyfin",
                table: "MediaFileStream",
                column: "MediaFileStream_MediaFileStreams_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Metadata_BookMetadata_BookMetadata_Id",
                schema: "jellyfin",
                table: "Metadata",
                column: "BookMetadata_BookMetadata_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Metadata_CompanyMetadata_CompanyMetadata_Id",
                schema: "jellyfin",
                table: "Metadata",
                column: "CompanyMetadata_CompanyMetadata_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Metadata_CustomItemMetadata_CustomItemMetadata_Id",
                schema: "jellyfin",
                table: "Metadata",
                column: "CustomItemMetadata_CustomItemMetadata_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Metadata_EpisodeMetadata_EpisodeMetadata_Id",
                schema: "jellyfin",
                table: "Metadata",
                column: "EpisodeMetadata_EpisodeMetadata_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Metadata_MovieMetadata_MovieMetadata_Id",
                schema: "jellyfin",
                table: "Metadata",
                column: "MovieMetadata_MovieMetadata_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Metadata_MusicAlbumMetadata_MusicAlbumMetadata_Id",
                schema: "jellyfin",
                table: "Metadata",
                column: "MusicAlbumMetadata_MusicAlbumMetadata_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Metadata_PhotoMetadata_PhotoMetadata_Id",
                schema: "jellyfin",
                table: "Metadata",
                column: "PhotoMetadata_PhotoMetadata_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Metadata_SeasonMetadata_SeasonMetadata_Id",
                schema: "jellyfin",
                table: "Metadata",
                column: "SeasonMetadata_SeasonMetadata_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Metadata_SeriesMetadata_SeriesMetadata_Id",
                schema: "jellyfin",
                table: "Metadata",
                column: "SeriesMetadata_SeriesMetadata_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Metadata_TrackMetadata_TrackMetadata_Id",
                schema: "jellyfin",
                table: "Metadata",
                column: "TrackMetadata_TrackMetadata_Id");

            migrationBuilder.CreateIndex(
                name: "IX_MetadataProviderId_MetadataProviderId_Sources_Id",
                schema: "jellyfin",
                table: "MetadataProviderId",
                column: "MetadataProviderId_Sources_Id");

            migrationBuilder.CreateIndex(
                name: "IX_MetadataProviderId_MetadataProvider_Id",
                schema: "jellyfin",
                table: "MetadataProviderId",
                column: "MetadataProvider_Id");

            migrationBuilder.CreateIndex(
                name: "IX_MetadataProviderId_PersonRole_PersonRoles_Id",
                schema: "jellyfin",
                table: "MetadataProviderId",
                column: "PersonRole_PersonRoles_Id");

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
                name: "IX_PersonRole_Artwork_Artwork_Id",
                schema: "jellyfin",
                table: "PersonRole",
                column: "Artwork_Artwork_Id");

            migrationBuilder.CreateIndex(
                name: "IX_PersonRole_PersonRole_PersonRoles_Id",
                schema: "jellyfin",
                table: "PersonRole",
                column: "PersonRole_PersonRoles_Id");

            migrationBuilder.CreateIndex(
                name: "IX_PersonRole_Person_Id",
                schema: "jellyfin",
                table: "PersonRole",
                column: "Person_Id");

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

            migrationBuilder.CreateIndex(
                name: "IX_Rating_PersonRole_PersonRoles_Id",
                schema: "jellyfin",
                table: "Rating",
                column: "PersonRole_PersonRoles_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Rating_RatingSource_RatingType_Id",
                schema: "jellyfin",
                table: "Rating",
                column: "RatingSource_RatingType_Id");

            migrationBuilder.CreateIndex(
                name: "IX_RatingSource_MetadataProviderId_Source_Id",
                schema: "jellyfin",
                table: "RatingSource",
                column: "MetadataProviderId_Source_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Release_Release_Releases_Id",
                schema: "jellyfin",
                table: "Release",
                column: "Release_Releases_Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PersonRole_Metadata_PersonRole_PersonRoles_Id",
                schema: "jellyfin",
                table: "PersonRole",
                column: "PersonRole_PersonRoles_Id",
                principalSchema: "jellyfin",
                principalTable: "Metadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PersonRole_Artwork_Artwork_Artwork_Id",
                schema: "jellyfin",
                table: "PersonRole",
                column: "Artwork_Artwork_Id",
                principalSchema: "jellyfin",
                principalTable: "Artwork",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Metadata_Company_CompanyMetadata_CompanyMetadata_Id",
                schema: "jellyfin",
                table: "Metadata",
                column: "CompanyMetadata_CompanyMetadata_Id",
                principalSchema: "jellyfin",
                principalTable: "Company",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Company_Metadata_Company_Labels_Id",
                schema: "jellyfin",
                table: "Company");

            migrationBuilder.DropForeignKey(
                name: "FK_Company_Metadata_Company_Networks_Id",
                schema: "jellyfin",
                table: "Company");

            migrationBuilder.DropForeignKey(
                name: "FK_Company_Metadata_Company_Publishers_Id",
                schema: "jellyfin",
                table: "Company");

            migrationBuilder.DropForeignKey(
                name: "FK_Company_Metadata_Company_Studios_Id",
                schema: "jellyfin",
                table: "Company");

            migrationBuilder.DropTable(
                name: "ActivityLog",
                schema: "jellyfin");

            migrationBuilder.DropTable(
                name: "Chapter",
                schema: "jellyfin");

            migrationBuilder.DropTable(
                name: "CollectionItem",
                schema: "jellyfin");

            migrationBuilder.DropTable(
                name: "Genre",
                schema: "jellyfin");

            migrationBuilder.DropTable(
                name: "MediaFileStream",
                schema: "jellyfin");

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
                name: "Rating",
                schema: "jellyfin");

            migrationBuilder.DropTable(
                name: "Collection",
                schema: "jellyfin");

            migrationBuilder.DropTable(
                name: "MediaFile",
                schema: "jellyfin");

            migrationBuilder.DropTable(
                name: "Group",
                schema: "jellyfin");

            migrationBuilder.DropTable(
                name: "RatingSource",
                schema: "jellyfin");

            migrationBuilder.DropTable(
                name: "Release",
                schema: "jellyfin");

            migrationBuilder.DropTable(
                name: "User",
                schema: "jellyfin");

            migrationBuilder.DropTable(
                name: "MetadataProviderId",
                schema: "jellyfin");

            migrationBuilder.DropTable(
                name: "PersonRole",
                schema: "jellyfin");

            migrationBuilder.DropTable(
                name: "MetadataProvider",
                schema: "jellyfin");

            migrationBuilder.DropTable(
                name: "Artwork",
                schema: "jellyfin");

            migrationBuilder.DropTable(
                name: "Person",
                schema: "jellyfin");

            migrationBuilder.DropTable(
                name: "Metadata",
                schema: "jellyfin");

            migrationBuilder.DropTable(
                name: "LibraryItem",
                schema: "jellyfin");

            migrationBuilder.DropTable(
                name: "Company",
                schema: "jellyfin");

            migrationBuilder.DropTable(
                name: "LibraryRoot",
                schema: "jellyfin");

            migrationBuilder.DropTable(
                name: "Library",
                schema: "jellyfin");
        }
    }
}
