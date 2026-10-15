using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Database.Providers.MySql.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase(
                collation: "utf8mb4_0900_bin")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ActivityLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", maxLength: 512, nullable: false, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Overview = table.Column<string>(type: "longtext", maxLength: 512, nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ShortOverview = table.Column<string>(type: "longtext", maxLength: 512, nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<string>(type: "longtext", maxLength: 256, nullable: false, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<Guid>(type: "binary(16)", nullable: false),
                    ItemId = table.Column<string>(type: "longtext", maxLength: 256, nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateCreated = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LogSeverity = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLogs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_bin");

            migrationBuilder.CreateTable(
                name: "ApiKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DateCreated = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateLastActivity = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Name = table.Column<string>(type: "longtext", maxLength: 64, nullable: false, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AccessToken = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_bin");

            migrationBuilder.CreateTable(
                name: "BaseItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "binary(16)", nullable: false),
                    Type = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Data = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Path = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StartDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ChannelId = table.Column<Guid>(type: "binary(16)", nullable: true),
                    IsMovie = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CommunityRating = table.Column<float>(type: "float", nullable: true),
                    CustomRating = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IndexNumber = table.Column<int>(type: "int", nullable: true),
                    IsLocked = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OfficialRating = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MediaType = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Overview = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ParentIndexNumber = table.Column<int>(type: "int", nullable: true),
                    PremiereDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ProductionYear = table.Column<int>(type: "int", nullable: true),
                    Genres = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortName = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ForcedSortName = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RunTimeTicks = table.Column<long>(type: "bigint", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsSeries = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    EpisodeTitle = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsRepeat = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PreferredMetadataLanguage = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PreferredMetadataCountryCode = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateLastRefreshed = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DateLastSaved = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsInMixedFolder = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Studios = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExternalServiceId = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tags = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsFolder = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    InheritedParentalRatingValue = table.Column<int>(type: "int", nullable: true),
                    InheritedParentalRatingSubValue = table.Column<int>(type: "int", nullable: true),
                    UnratedType = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CriticRating = table.Column<float>(type: "float", nullable: true),
                    CleanName = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PresentationUniqueKey = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OriginalTitle = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CleanOriginalTitle = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OriginalLanguage = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PrimaryVersionId = table.Column<Guid>(type: "binary(16)", nullable: true),
                    DateLastMediaAdded = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Album = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LUFS = table.Column<float>(type: "float", nullable: true),
                    NormalizationGain = table.Column<float>(type: "float", nullable: true),
                    IsVirtualItem = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SeriesName = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SeasonName = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExternalSeriesId = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tagline = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProductionLocations = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TotalBitrate = table.Column<int>(type: "int", nullable: true),
                    ExtraType = table.Column<int>(type: "int", nullable: true),
                    Artists = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AlbumArtists = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExternalId = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SeriesPresentationUniqueKey = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ShowId = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OwnerId = table.Column<Guid>(type: "binary(16)", nullable: true),
                    Width = table.Column<int>(type: "int", nullable: true),
                    Height = table.Column<int>(type: "int", nullable: true),
                    Size = table.Column<long>(type: "bigint", nullable: true),
                    Audio = table.Column<int>(type: "int", nullable: true),
                    ParentId = table.Column<Guid>(type: "binary(16)", nullable: true),
                    TopParentId = table.Column<Guid>(type: "binary(16)", nullable: true),
                    SeasonId = table.Column<Guid>(type: "binary(16)", nullable: true),
                    SeriesId = table.Column<Guid>(type: "binary(16)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaseItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BaseItems_BaseItems_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "BaseItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BaseItems_BaseItems_ParentId",
                        column: x => x.ParentId,
                        principalTable: "BaseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_bin");

            migrationBuilder.CreateTable(
                name: "CustomItemDisplayPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<Guid>(type: "binary(16)", nullable: false),
                    ItemId = table.Column<Guid>(type: "binary(16)", nullable: false),
                    Client = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Key = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Value = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomItemDisplayPreferences", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_bin");

            migrationBuilder.CreateTable(
                name: "DeviceOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DeviceId = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CustomName = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceOptions", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_bin");

            migrationBuilder.CreateTable(
                name: "ItemValues",
                columns: table => new
                {
                    ItemValueId = table.Column<Guid>(type: "binary(16)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CleanValue = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemValues", x => x.ItemValueId);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_bin");

            migrationBuilder.CreateTable(
                name: "MediaSegments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "binary(16)", nullable: false),
                    ItemId = table.Column<Guid>(type: "binary(16)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    EndTicks = table.Column<long>(type: "bigint", nullable: false),
                    StartTicks = table.Column<long>(type: "bigint", nullable: false),
                    SegmentProviderId = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaSegments", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_bin");

            migrationBuilder.CreateTable(
                name: "Peoples",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "binary(16)", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NameLower = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PersonType = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Peoples", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_bin");

            migrationBuilder.CreateTable(
                name: "TrickplayInfos",
                columns: table => new
                {
                    ItemId = table.Column<Guid>(type: "binary(16)", nullable: false),
                    Width = table.Column<int>(type: "int", nullable: false),
                    Height = table.Column<int>(type: "int", nullable: false),
                    TileWidth = table.Column<int>(type: "int", nullable: false),
                    TileHeight = table.Column<int>(type: "int", nullable: false),
                    ThumbnailCount = table.Column<int>(type: "int", nullable: false),
                    Interval = table.Column<int>(type: "int", nullable: false),
                    Bandwidth = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrickplayInfos", x => new { x.ItemId, x.Width });
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_bin");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "binary(16)", nullable: false),
                    Username = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NormalizedUsername = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Password = table.Column<string>(type: "longtext", maxLength: 65535, nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MustUpdatePassword = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AudioLanguagePreference = table.Column<string>(type: "longtext", maxLength: 255, nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AuthenticationProviderId = table.Column<string>(type: "longtext", maxLength: 255, nullable: false, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PasswordResetProviderId = table.Column<string>(type: "longtext", maxLength: 255, nullable: false, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InvalidLoginAttemptCount = table.Column<int>(type: "int", nullable: false),
                    LastActivityDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastLoginDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LoginAttemptsBeforeLockout = table.Column<int>(type: "int", nullable: true),
                    MaxActiveSessions = table.Column<int>(type: "int", nullable: false),
                    SubtitleMode = table.Column<int>(type: "int", nullable: false),
                    PlayDefaultAudioTrack = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SubtitleLanguagePreference = table.Column<string>(type: "longtext", maxLength: 255, nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayMissingEpisodes = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DisplayCollectionsView = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    EnableLocalPassword = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    HidePlayedInLatest = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RememberAudioSelections = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RememberSubtitleSelections = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    EnableNextEpisodeAutoPlay = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    EnableAutoLogin = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    EnableUserPreferenceAccess = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MaxParentalRatingScore = table.Column<int>(type: "int", nullable: true),
                    MaxParentalRatingSubScore = table.Column<int>(type: "int", nullable: true),
                    RemoteClientBitrateLimit = table.Column<int>(type: "int", nullable: true),
                    InternalId = table.Column<long>(type: "bigint", nullable: false),
                    SyncPlayAccess = table.Column<int>(type: "int", nullable: false),
                    CastReceiverId = table.Column<string>(type: "longtext", maxLength: 32, nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RowVersion = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_bin");

            migrationBuilder.CreateTable(
                name: "AncestorIds",
                columns: table => new
                {
                    ParentItemId = table.Column<Guid>(type: "binary(16)", nullable: false),
                    ItemId = table.Column<Guid>(type: "binary(16)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AncestorIds", x => new { x.ItemId, x.ParentItemId });
                    table.ForeignKey(
                        name: "FK_AncestorIds_BaseItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "BaseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AncestorIds_BaseItems_ParentItemId",
                        column: x => x.ParentItemId,
                        principalTable: "BaseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_bin");

            migrationBuilder.CreateTable(
                name: "AttachmentStreamInfos",
                columns: table => new
                {
                    ItemId = table.Column<Guid>(type: "binary(16)", nullable: false),
                    Index = table.Column<int>(type: "int", nullable: false),
                    Codec = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CodecTag = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Comment = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Filename = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MimeType = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttachmentStreamInfos", x => new { x.ItemId, x.Index });
                    table.ForeignKey(
                        name: "FK_AttachmentStreamInfos_BaseItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "BaseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_bin");

            migrationBuilder.CreateTable(
                name: "BaseItemImageInfos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "binary(16)", nullable: false),
                    Path = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateModified = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ImageType = table.Column<int>(type: "int", nullable: false),
                    Width = table.Column<int>(type: "int", nullable: false),
                    Height = table.Column<int>(type: "int", nullable: false),
                    Blurhash = table.Column<byte[]>(type: "longblob", nullable: true),
                    ItemId = table.Column<Guid>(type: "binary(16)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaseItemImageInfos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BaseItemImageInfos_BaseItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "BaseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_bin");

            migrationBuilder.CreateTable(
                name: "BaseItemMetadataFields",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<Guid>(type: "binary(16)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaseItemMetadataFields", x => new { x.Id, x.ItemId });
                    table.ForeignKey(
                        name: "FK_BaseItemMetadataFields_BaseItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "BaseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_bin");

            migrationBuilder.CreateTable(
                name: "BaseItemProviders",
                columns: table => new
                {
                    ItemId = table.Column<Guid>(type: "binary(16)", nullable: false),
                    ProviderId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProviderValue = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaseItemProviders", x => new { x.ItemId, x.ProviderId });
                    table.ForeignKey(
                        name: "FK_BaseItemProviders_BaseItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "BaseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_bin");

            migrationBuilder.CreateTable(
                name: "BaseItemTrailerTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<Guid>(type: "binary(16)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaseItemTrailerTypes", x => new { x.Id, x.ItemId });
                    table.ForeignKey(
                        name: "FK_BaseItemTrailerTypes_BaseItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "BaseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_bin");

            migrationBuilder.CreateTable(
                name: "Chapters",
                columns: table => new
                {
                    ItemId = table.Column<Guid>(type: "binary(16)", nullable: false),
                    ChapterIndex = table.Column<int>(type: "int", nullable: false),
                    StartPositionTicks = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ImagePath = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ImageDateModified = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chapters", x => new { x.ItemId, x.ChapterIndex });
                    table.ForeignKey(
                        name: "FK_Chapters_BaseItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "BaseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_bin");

            migrationBuilder.CreateTable(
                name: "KeyframeData",
                columns: table => new
                {
                    ItemId = table.Column<Guid>(type: "binary(16)", nullable: false),
                    TotalDuration = table.Column<long>(type: "bigint", nullable: false),
                    KeyframeTicks = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeyframeData", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_KeyframeData_BaseItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "BaseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_bin");

            migrationBuilder.CreateTable(
                name: "LinkedChildren",
                columns: table => new
                {
                    ParentId = table.Column<Guid>(type: "binary(16)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    ChildId = table.Column<Guid>(type: "binary(16)", nullable: false),
                    ChildType = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LinkedChildren", x => new { x.ParentId, x.SortOrder });
                    table.ForeignKey(
                        name: "FK_LinkedChildren_BaseItems_ChildId",
                        column: x => x.ChildId,
                        principalTable: "BaseItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LinkedChildren_BaseItems_ParentId",
                        column: x => x.ParentId,
                        principalTable: "BaseItems",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_bin");

            migrationBuilder.CreateTable(
                name: "MediaStreamInfos",
                columns: table => new
                {
                    ItemId = table.Column<Guid>(type: "binary(16)", nullable: false),
                    StreamIndex = table.Column<int>(type: "int", nullable: false),
                    StreamType = table.Column<int>(type: "int", nullable: false),
                    Codec = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Language = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChannelLayout = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Profile = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AspectRatio = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Path = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsInterlaced = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    BitRate = table.Column<int>(type: "int", nullable: true),
                    Channels = table.Column<int>(type: "int", nullable: true),
                    SampleRate = table.Column<int>(type: "int", nullable: true),
                    IsDefault = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsForced = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsExternal = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsOriginal = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Height = table.Column<int>(type: "int", nullable: true),
                    Width = table.Column<int>(type: "int", nullable: true),
                    AverageFrameRate = table.Column<float>(type: "float", nullable: true),
                    RealFrameRate = table.Column<float>(type: "float", nullable: true),
                    Level = table.Column<float>(type: "float", nullable: true),
                    PixelFormat = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BitDepth = table.Column<int>(type: "int", nullable: true),
                    IsAnamorphic = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    RefFrames = table.Column<int>(type: "int", nullable: true),
                    CodecTag = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Comment = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NalLengthSize = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsAvc = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    Title = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TimeBase = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CodecTimeBase = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ColorPrimaries = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ColorSpace = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ColorTransfer = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DvVersionMajor = table.Column<int>(type: "int", nullable: true),
                    DvVersionMinor = table.Column<int>(type: "int", nullable: true),
                    DvProfile = table.Column<int>(type: "int", nullable: true),
                    DvLevel = table.Column<int>(type: "int", nullable: true),
                    RpuPresentFlag = table.Column<int>(type: "int", nullable: true),
                    ElPresentFlag = table.Column<int>(type: "int", nullable: true),
                    BlPresentFlag = table.Column<int>(type: "int", nullable: true),
                    DvBlSignalCompatibilityId = table.Column<int>(type: "int", nullable: true),
                    IsHearingImpaired = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    Rotation = table.Column<int>(type: "int", nullable: true),
                    KeyFrames = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Hdr10PlusPresentFlag = table.Column<bool>(type: "tinyint(1)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaStreamInfos", x => new { x.ItemId, x.StreamIndex });
                    table.ForeignKey(
                        name: "FK_MediaStreamInfos_BaseItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "BaseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_bin");

            migrationBuilder.CreateTable(
                name: "ItemValuesMap",
                columns: table => new
                {
                    ItemId = table.Column<Guid>(type: "binary(16)", nullable: false),
                    ItemValueId = table.Column<Guid>(type: "binary(16)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemValuesMap", x => new { x.ItemValueId, x.ItemId });
                    table.ForeignKey(
                        name: "FK_ItemValuesMap_BaseItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "BaseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemValuesMap_ItemValues_ItemValueId",
                        column: x => x.ItemValueId,
                        principalTable: "ItemValues",
                        principalColumn: "ItemValueId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_bin");

            migrationBuilder.CreateTable(
                name: "PeopleBaseItemMap",
                columns: table => new
                {
                    Role = table.Column<string>(type: "varchar(400)", maxLength: 400, nullable: false, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ItemId = table.Column<Guid>(type: "binary(16)", nullable: false),
                    PeopleId = table.Column<Guid>(type: "binary(16)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: true),
                    ListOrder = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeopleBaseItemMap", x => new { x.ItemId, x.PeopleId, x.Role });
                    table.ForeignKey(
                        name: "FK_PeopleBaseItemMap_BaseItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "BaseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PeopleBaseItemMap_Peoples_PeopleId",
                        column: x => x.PeopleId,
                        principalTable: "Peoples",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_bin");

            migrationBuilder.CreateTable(
                name: "AccessSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<Guid>(type: "binary(16)", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    StartHour = table.Column<double>(type: "double", nullable: false),
                    EndHour = table.Column<double>(type: "double", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccessSchedules_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_bin");

            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<Guid>(type: "binary(16)", nullable: false),
                    AccessToken = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AppName = table.Column<string>(type: "longtext", maxLength: 64, nullable: false, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AppVersion = table.Column<string>(type: "longtext", maxLength: 32, nullable: false, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeviceName = table.Column<string>(type: "longtext", maxLength: 64, nullable: false, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeviceId = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateModified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateLastActivity = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Devices_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_bin");

            migrationBuilder.CreateTable(
                name: "DisplayPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<Guid>(type: "binary(16)", nullable: false),
                    ItemId = table.Column<Guid>(type: "binary(16)", nullable: false),
                    Client = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ShowSidebar = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ShowBackdrop = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ScrollDirection = table.Column<int>(type: "int", nullable: false),
                    IndexBy = table.Column<int>(type: "int", nullable: true),
                    SkipForwardLength = table.Column<int>(type: "int", nullable: false),
                    SkipBackwardLength = table.Column<int>(type: "int", nullable: false),
                    ChromecastVersion = table.Column<int>(type: "int", nullable: false),
                    EnableNextVideoInfoOverlay = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DashboardTheme = table.Column<string>(type: "longtext", maxLength: 32, nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TvHome = table.Column<string>(type: "longtext", maxLength: 32, nullable: true, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisplayPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DisplayPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_bin");

            migrationBuilder.CreateTable(
                name: "ImageInfos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<Guid>(type: "binary(16)", nullable: true),
                    Path = table.Column<string>(type: "longtext", maxLength: 512, nullable: false, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastModified = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageInfos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImageInfos_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_bin");

            migrationBuilder.CreateTable(
                name: "ItemDisplayPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<Guid>(type: "binary(16)", nullable: false),
                    ItemId = table.Column<Guid>(type: "binary(16)", nullable: false),
                    Client = table.Column<string>(type: "longtext", maxLength: 32, nullable: false, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ViewType = table.Column<int>(type: "int", nullable: false),
                    RememberIndexing = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IndexBy = table.Column<int>(type: "int", nullable: true),
                    RememberSorting = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SortBy = table.Column<string>(type: "longtext", maxLength: 64, nullable: false, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemDisplayPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemDisplayPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_bin");

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<Guid>(type: "binary(16)", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RowVersion = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Permissions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_bin");

            migrationBuilder.CreateTable(
                name: "Preferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<Guid>(type: "binary(16)", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "longtext", maxLength: 65535, nullable: false, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RowVersion = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Preferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Preferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_bin");

            migrationBuilder.CreateTable(
                name: "UserData",
                columns: table => new
                {
                    CustomDataKey = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false, collation: "utf8mb4_0900_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ItemId = table.Column<Guid>(type: "binary(16)", nullable: false),
                    UserId = table.Column<Guid>(type: "binary(16)", nullable: false),
                    Rating = table.Column<double>(type: "double", nullable: true),
                    PlaybackPositionTicks = table.Column<long>(type: "bigint", nullable: false),
                    PlayCount = table.Column<int>(type: "int", nullable: false),
                    IsFavorite = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LastPlayedDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Played = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AudioStreamIndex = table.Column<int>(type: "int", nullable: true),
                    SubtitleStreamIndex = table.Column<int>(type: "int", nullable: true),
                    Likes = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    RetentionDate = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserData", x => new { x.ItemId, x.UserId, x.CustomDataKey });
                    table.ForeignKey(
                        name: "FK_UserData_BaseItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "BaseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserData_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_bin");

            migrationBuilder.CreateTable(
                name: "HomeSection",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DisplayPreferencesId = table.Column<int>(type: "int", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomeSection", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HomeSection_DisplayPreferences_DisplayPreferencesId",
                        column: x => x.DisplayPreferencesId,
                        principalTable: "DisplayPreferences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_bin");

            migrationBuilder.InsertData(
                table: "BaseItems",
                columns: new[] { "Id", "Album", "AlbumArtists", "Artists", "Audio", "ChannelId", "CleanName", "CleanOriginalTitle", "CommunityRating", "CriticRating", "CustomRating", "Data", "DateCreated", "DateLastMediaAdded", "DateLastRefreshed", "DateLastSaved", "DateModified", "EndDate", "EpisodeTitle", "ExternalId", "ExternalSeriesId", "ExternalServiceId", "ExtraType", "ForcedSortName", "Genres", "Height", "IndexNumber", "InheritedParentalRatingSubValue", "InheritedParentalRatingValue", "IsFolder", "IsInMixedFolder", "IsLocked", "IsMovie", "IsRepeat", "IsSeries", "IsVirtualItem", "LUFS", "MediaType", "Name", "NormalizationGain", "OfficialRating", "OriginalLanguage", "OriginalTitle", "Overview", "OwnerId", "ParentId", "ParentIndexNumber", "Path", "PreferredMetadataCountryCode", "PreferredMetadataLanguage", "PremiereDate", "PresentationUniqueKey", "PrimaryVersionId", "ProductionLocations", "ProductionYear", "RunTimeTicks", "SeasonId", "SeasonName", "SeriesId", "SeriesName", "SeriesPresentationUniqueKey", "ShowId", "Size", "SortName", "StartDate", "Studios", "Tagline", "Tags", "TopParentId", "TotalBitrate", "Type", "UnratedType", "Width" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, false, false, false, false, false, false, false, null, null, "This is a placeholder item for UserData that has been detached from its original item", null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, "PLACEHOLDER", null, null });

            migrationBuilder.CreateIndex(
                name: "IX_AccessSchedules_UserId",
                table: "AccessSchedules",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_DateCreated",
                table: "ActivityLogs",
                column: "DateCreated");

            migrationBuilder.CreateIndex(
                name: "IX_AncestorIds_ParentItemId",
                table: "AncestorIds",
                column: "ParentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_AccessToken",
                table: "ApiKeys",
                column: "AccessToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BaseItemImageInfos_ItemId_ImageType",
                table: "BaseItemImageInfos",
                columns: new[] { "ItemId", "ImageType" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItemMetadataFields_ItemId",
                table: "BaseItemMetadataFields",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_BaseItemProviders_ProviderId_ItemId_ProviderValue",
                table: "BaseItemProviders",
                columns: new[] { "ProviderId", "ItemId", "ProviderValue" })
                .Annotation("MySql:IndexPrefixLength", new[] { 0, 0, 512 });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_ExtraType_OwnerId",
                table: "BaseItems",
                columns: new[] { "ExtraType", "OwnerId" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_IsFolder_TopParentId_IsVirtualItem_Present_d385e7f8",
                table: "BaseItems",
                columns: new[] { "IsFolder", "TopParentId", "IsVirtualItem", "PresentationUniqueKey", "DateCreated" })
                .Annotation("MySql:IndexPrefixLength", new[] { 0, 0, 0, 512, 0 });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_MediaType_TopParentId_IsVirtualItem_Presen_20a0002d",
                table: "BaseItems",
                columns: new[] { "MediaType", "TopParentId", "IsVirtualItem", "PresentationUniqueKey" })
                .Annotation("MySql:IndexPrefixLength", new[] { 0, 0, 0, 512 });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Name",
                table: "BaseItems",
                column: "Name")
                .Annotation("MySql:IndexPrefixLength", new[] { 512 });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_OwnerId",
                table: "BaseItems",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_ParentId",
                table: "BaseItems",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Path",
                table: "BaseItems",
                column: "Path")
                .Annotation("MySql:IndexPrefixLength", new[] { 512 });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_PresentationUniqueKey",
                table: "BaseItems",
                column: "PresentationUniqueKey")
                .Annotation("MySql:IndexPrefixLength", new[] { 512 });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_PrimaryVersionId",
                table: "BaseItems",
                column: "PrimaryVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_SeasonId",
                table: "BaseItems",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_SeriesId",
                table: "BaseItems",
                column: "SeriesId");

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_SeriesName",
                table: "BaseItems",
                column: "SeriesName")
                .Annotation("MySql:IndexPrefixLength", new[] { 512 });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_TopParentId_Id",
                table: "BaseItems",
                columns: new[] { "TopParentId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_TopParentId_IsFolder_IsVirtualItem_DateCreated",
                table: "BaseItems",
                columns: new[] { "TopParentId", "IsFolder", "IsVirtualItem", "DateCreated" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_TopParentId_MediaType_IsVirtualItem_DateCreated",
                table: "BaseItems",
                columns: new[] { "TopParentId", "MediaType", "IsVirtualItem", "DateCreated" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_TopParentId_Type_IsVirtualItem",
                table: "BaseItems",
                columns: new[] { "TopParentId", "Type", "IsVirtualItem" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_TopParentId_Type_IsVirtualItem_DateCreated",
                table: "BaseItems",
                columns: new[] { "TopParentId", "Type", "IsVirtualItem", "DateCreated" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Type_CleanName",
                table: "BaseItems",
                columns: new[] { "Type", "CleanName" })
                .Annotation("MySql:IndexPrefixLength", new[] { 0, 512 });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Type_SeriesPresentationUniqueKey_IsFolder__8661462a",
                table: "BaseItems",
                columns: new[] { "Type", "SeriesPresentationUniqueKey", "IsFolder", "IsVirtualItem" })
                .Annotation("MySql:IndexPrefixLength", new[] { 0, 511, 0, 0 });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Type_SeriesPresentationUniqueKey_ParentInd_f161c196",
                table: "BaseItems",
                columns: new[] { "Type", "SeriesPresentationUniqueKey", "ParentIndexNumber", "IndexNumber" })
                .Annotation("MySql:IndexPrefixLength", new[] { 0, 510, 0, 0 });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Type_SeriesPresentationUniqueKey_Presentat_d243c44a",
                table: "BaseItems",
                columns: new[] { "Type", "SeriesPresentationUniqueKey", "PresentationUniqueKey", "SortName" })
                .Annotation("MySql:IndexPrefixLength", new[] { 0, 170, 170, 170 });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Type_TopParentId_Id",
                table: "BaseItems",
                columns: new[] { "Type", "TopParentId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Type_TopParentId_IsVirtualItem_Presentatio_5c120b7b",
                table: "BaseItems",
                columns: new[] { "Type", "TopParentId", "IsVirtualItem", "PresentationUniqueKey", "DateCreated" })
                .Annotation("MySql:IndexPrefixLength", new[] { 0, 0, 0, 505, 0 });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Type_TopParentId_PresentationUniqueKey",
                table: "BaseItems",
                columns: new[] { "Type", "TopParentId", "PresentationUniqueKey" })
                .Annotation("MySql:IndexPrefixLength", new[] { 0, 0, 508 });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Type_TopParentId_SortName",
                table: "BaseItems",
                columns: new[] { "Type", "TopParentId", "SortName" })
                .Annotation("MySql:IndexPrefixLength", new[] { 0, 0, 508 });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Type_TopParentId_StartDate",
                table: "BaseItems",
                columns: new[] { "Type", "TopParentId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItemTrailerTypes_ItemId",
                table: "BaseItemTrailerTypes",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomItemDisplayPreferences_UserId_ItemId_Client_Key",
                table: "CustomItemDisplayPreferences",
                columns: new[] { "UserId", "ItemId", "Client", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceOptions_DeviceId",
                table: "DeviceOptions",
                column: "DeviceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devices_AccessToken_DateLastActivity",
                table: "Devices",
                columns: new[] { "AccessToken", "DateLastActivity" });

            migrationBuilder.CreateIndex(
                name: "IX_Devices_DeviceId_DateLastActivity",
                table: "Devices",
                columns: new[] { "DeviceId", "DateLastActivity" });

            migrationBuilder.CreateIndex(
                name: "IX_Devices_UserId_DeviceId",
                table: "Devices",
                columns: new[] { "UserId", "DeviceId" });

            migrationBuilder.CreateIndex(
                name: "IX_DisplayPreferences_UserId_ItemId_Client",
                table: "DisplayPreferences",
                columns: new[] { "UserId", "ItemId", "Client" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HomeSection_DisplayPreferencesId",
                table: "HomeSection",
                column: "DisplayPreferencesId");

            migrationBuilder.CreateIndex(
                name: "IX_ImageInfos_UserId",
                table: "ImageInfos",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemDisplayPreferences_UserId",
                table: "ItemDisplayPreferences",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemValues_Type_CleanValue",
                table: "ItemValues",
                columns: new[] { "Type", "CleanValue" })
                .Annotation("MySql:IndexPrefixLength", new[] { 0, 512 });

            migrationBuilder.CreateIndex(
                name: "IX_ItemValues_Type_Value",
                table: "ItemValues",
                columns: new[] { "Type", "Value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemValuesMap_ItemId",
                table: "ItemValuesMap",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_LinkedChildren_ChildId_ChildType",
                table: "LinkedChildren",
                columns: new[] { "ChildId", "ChildType" });

            migrationBuilder.CreateIndex(
                name: "IX_LinkedChildren_ParentId_ChildType",
                table: "LinkedChildren",
                columns: new[] { "ParentId", "ChildType" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaStreamInfos_StreamType_ItemId_Language_IsExternal",
                table: "MediaStreamInfos",
                columns: new[] { "StreamType", "ItemId", "Language", "IsExternal" })
                .Annotation("MySql:IndexPrefixLength", new[] { 0, 0, 512, 0 });

            migrationBuilder.CreateIndex(
                name: "IX_PeopleBaseItemMap_ItemId_ListOrder",
                table: "PeopleBaseItemMap",
                columns: new[] { "ItemId", "ListOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_PeopleBaseItemMap_ItemId_SortOrder",
                table: "PeopleBaseItemMap",
                columns: new[] { "ItemId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_PeopleBaseItemMap_PeopleId_ItemId",
                table: "PeopleBaseItemMap",
                columns: new[] { "PeopleId", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_Peoples_Name",
                table: "Peoples",
                column: "Name")
                .Annotation("MySql:IndexPrefixLength", new[] { 512 });

            migrationBuilder.CreateIndex(
                name: "IX_Peoples_NameLower",
                table: "Peoples",
                column: "NameLower")
                .Annotation("MySql:IndexPrefixLength", new[] { 512 });

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_UserId_Kind",
                table: "Permissions",
                columns: new[] { "UserId", "Kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Preferences_UserId_Kind",
                table: "Preferences",
                columns: new[] { "UserId", "Kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserData_ItemId_UserId_IsFavorite",
                table: "UserData",
                columns: new[] { "ItemId", "UserId", "IsFavorite" });

            migrationBuilder.CreateIndex(
                name: "IX_UserData_ItemId_UserId_LastPlayedDate",
                table: "UserData",
                columns: new[] { "ItemId", "UserId", "LastPlayedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_UserData_ItemId_UserId_PlaybackPositionTicks",
                table: "UserData",
                columns: new[] { "ItemId", "UserId", "PlaybackPositionTicks" });

            migrationBuilder.CreateIndex(
                name: "IX_UserData_ItemId_UserId_Played",
                table: "UserData",
                columns: new[] { "ItemId", "UserId", "Played" });

            migrationBuilder.CreateIndex(
                name: "IX_UserData_UserId_IsFavorite_ItemId",
                table: "UserData",
                columns: new[] { "UserId", "IsFavorite", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserData_UserId_ItemId_LastPlayedDate",
                table: "UserData",
                columns: new[] { "UserId", "ItemId", "LastPlayedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_UserData_UserId_Played_ItemId",
                table: "UserData",
                columns: new[] { "UserId", "Played", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_NormalizedUsername",
                table: "Users",
                column: "NormalizedUsername",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessSchedules");

            migrationBuilder.DropTable(
                name: "ActivityLogs");

            migrationBuilder.DropTable(
                name: "AncestorIds");

            migrationBuilder.DropTable(
                name: "ApiKeys");

            migrationBuilder.DropTable(
                name: "AttachmentStreamInfos");

            migrationBuilder.DropTable(
                name: "BaseItemImageInfos");

            migrationBuilder.DropTable(
                name: "BaseItemMetadataFields");

            migrationBuilder.DropTable(
                name: "BaseItemProviders");

            migrationBuilder.DropTable(
                name: "BaseItemTrailerTypes");

            migrationBuilder.DropTable(
                name: "Chapters");

            migrationBuilder.DropTable(
                name: "CustomItemDisplayPreferences");

            migrationBuilder.DropTable(
                name: "DeviceOptions");

            migrationBuilder.DropTable(
                name: "Devices");

            migrationBuilder.DropTable(
                name: "HomeSection");

            migrationBuilder.DropTable(
                name: "ImageInfos");

            migrationBuilder.DropTable(
                name: "ItemDisplayPreferences");

            migrationBuilder.DropTable(
                name: "ItemValuesMap");

            migrationBuilder.DropTable(
                name: "KeyframeData");

            migrationBuilder.DropTable(
                name: "LinkedChildren");

            migrationBuilder.DropTable(
                name: "MediaSegments");

            migrationBuilder.DropTable(
                name: "MediaStreamInfos");

            migrationBuilder.DropTable(
                name: "PeopleBaseItemMap");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "Preferences");

            migrationBuilder.DropTable(
                name: "TrickplayInfos");

            migrationBuilder.DropTable(
                name: "UserData");

            migrationBuilder.DropTable(
                name: "DisplayPreferences");

            migrationBuilder.DropTable(
                name: "ItemValues");

            migrationBuilder.DropTable(
                name: "Peoples");

            migrationBuilder.DropTable(
                name: "BaseItems");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.AlterDatabase(
                oldCollation: "utf8mb4_0900_bin")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
