using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Jellyfin.Database.Providers.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivityLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", maxLength: 512, nullable: false, collation: "C"),
                    Overview = table.Column<string>(type: "text", maxLength: 512, nullable: true, collation: "C"),
                    ShortOverview = table.Column<string>(type: "text", maxLength: 512, nullable: true, collation: "C"),
                    Type = table.Column<string>(type: "text", maxLength: 256, nullable: false, collation: "C"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<string>(type: "text", maxLength: 256, nullable: true, collation: "C"),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LogSeverity = table.Column<int>(type: "integer", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApiKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateLastActivity = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Name = table.Column<string>(type: "text", maxLength: 64, nullable: false, collation: "C"),
                    AccessToken = table.Column<string>(type: "text", nullable: false, collation: "C")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BaseItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false, collation: "C"),
                    Data = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    Path = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ChannelId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsMovie = table.Column<bool>(type: "boolean", nullable: false),
                    CommunityRating = table.Column<float>(type: "real", nullable: true),
                    CustomRating = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    IndexNumber = table.Column<int>(type: "integer", nullable: true),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    OfficialRating = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    MediaType = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    Overview = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    ParentIndexNumber = table.Column<int>(type: "integer", nullable: true),
                    PremiereDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProductionYear = table.Column<int>(type: "integer", nullable: true),
                    Genres = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    SortName = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    ForcedSortName = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    RunTimeTicks = table.Column<long>(type: "bigint", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DateModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsSeries = table.Column<bool>(type: "boolean", nullable: false),
                    EpisodeTitle = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    IsRepeat = table.Column<bool>(type: "boolean", nullable: false),
                    PreferredMetadataLanguage = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    PreferredMetadataCountryCode = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    DateLastRefreshed = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DateLastSaved = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsInMixedFolder = table.Column<bool>(type: "boolean", nullable: false),
                    Studios = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    ExternalServiceId = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    Tags = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    IsFolder = table.Column<bool>(type: "boolean", nullable: false),
                    InheritedParentalRatingValue = table.Column<int>(type: "integer", nullable: true),
                    InheritedParentalRatingSubValue = table.Column<int>(type: "integer", nullable: true),
                    UnratedType = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    CriticRating = table.Column<float>(type: "real", nullable: true),
                    CleanName = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    PresentationUniqueKey = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    OriginalTitle = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    OriginalLanguage = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    PrimaryVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    DateLastMediaAdded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Album = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    LUFS = table.Column<float>(type: "real", nullable: true),
                    NormalizationGain = table.Column<float>(type: "real", nullable: true),
                    IsVirtualItem = table.Column<bool>(type: "boolean", nullable: false),
                    SeriesName = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    SeasonName = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    ExternalSeriesId = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    Tagline = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    ProductionLocations = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    TotalBitrate = table.Column<int>(type: "integer", nullable: true),
                    ExtraType = table.Column<int>(type: "integer", nullable: true),
                    Artists = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    AlbumArtists = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    ExternalId = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    SeriesPresentationUniqueKey = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    ShowId = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Width = table.Column<int>(type: "integer", nullable: true),
                    Height = table.Column<int>(type: "integer", nullable: true),
                    Size = table.Column<long>(type: "bigint", nullable: true),
                    Audio = table.Column<int>(type: "integer", nullable: true),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    TopParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    SeasonId = table.Column<Guid>(type: "uuid", nullable: true),
                    SeriesId = table.Column<Guid>(type: "uuid", nullable: true)
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
                });

            migrationBuilder.CreateTable(
                name: "CustomItemDisplayPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Client = table.Column<string>(type: "text", maxLength: 32, nullable: false, collation: "C"),
                    Key = table.Column<string>(type: "text", nullable: false, collation: "C"),
                    Value = table.Column<string>(type: "text", nullable: true, collation: "C")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomItemDisplayPreferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeviceOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DeviceId = table.Column<string>(type: "text", nullable: false, collation: "C"),
                    CustomName = table.Column<string>(type: "text", nullable: true, collation: "C")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceOptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItemValues",
                columns: table => new
                {
                    ItemValueId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false, collation: "C"),
                    CleanValue = table.Column<string>(type: "text", nullable: false, collation: "C")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemValues", x => x.ItemValueId);
                });

            migrationBuilder.CreateTable(
                name: "MediaSegments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    EndTicks = table.Column<long>(type: "bigint", nullable: false),
                    StartTicks = table.Column<long>(type: "bigint", nullable: false),
                    SegmentProviderId = table.Column<string>(type: "text", nullable: false, collation: "C")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaSegments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Peoples",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false, collation: "C"),
                    PersonType = table.Column<string>(type: "text", nullable: true, collation: "C")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Peoples", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrickplayInfos",
                columns: table => new
                {
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: false),
                    Height = table.Column<int>(type: "integer", nullable: false),
                    TileWidth = table.Column<int>(type: "integer", nullable: false),
                    TileHeight = table.Column<int>(type: "integer", nullable: false),
                    ThumbnailCount = table.Column<int>(type: "integer", nullable: false),
                    Interval = table.Column<int>(type: "integer", nullable: false),
                    Bandwidth = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrickplayInfos", x => new { x.ItemId, x.Width });
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "text", maxLength: 255, nullable: false, collation: "C"),
                    NormalizedUsername = table.Column<string>(type: "text", maxLength: 255, nullable: false, collation: "C"),
                    Password = table.Column<string>(type: "text", maxLength: 65535, nullable: true, collation: "C"),
                    MustUpdatePassword = table.Column<bool>(type: "boolean", nullable: false),
                    AudioLanguagePreference = table.Column<string>(type: "text", maxLength: 255, nullable: true, collation: "C"),
                    AuthenticationProviderId = table.Column<string>(type: "text", maxLength: 255, nullable: false, collation: "C"),
                    PasswordResetProviderId = table.Column<string>(type: "text", maxLength: 255, nullable: false, collation: "C"),
                    InvalidLoginAttemptCount = table.Column<int>(type: "integer", nullable: false),
                    LastActivityDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastLoginDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LoginAttemptsBeforeLockout = table.Column<int>(type: "integer", nullable: true),
                    MaxActiveSessions = table.Column<int>(type: "integer", nullable: false),
                    SubtitleMode = table.Column<int>(type: "integer", nullable: false),
                    PlayDefaultAudioTrack = table.Column<bool>(type: "boolean", nullable: false),
                    SubtitleLanguagePreference = table.Column<string>(type: "text", maxLength: 255, nullable: true, collation: "C"),
                    DisplayMissingEpisodes = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayCollectionsView = table.Column<bool>(type: "boolean", nullable: false),
                    EnableLocalPassword = table.Column<bool>(type: "boolean", nullable: false),
                    HidePlayedInLatest = table.Column<bool>(type: "boolean", nullable: false),
                    RememberAudioSelections = table.Column<bool>(type: "boolean", nullable: false),
                    RememberSubtitleSelections = table.Column<bool>(type: "boolean", nullable: false),
                    EnableNextEpisodeAutoPlay = table.Column<bool>(type: "boolean", nullable: false),
                    EnableAutoLogin = table.Column<bool>(type: "boolean", nullable: false),
                    EnableUserPreferenceAccess = table.Column<bool>(type: "boolean", nullable: false),
                    MaxParentalRatingScore = table.Column<int>(type: "integer", nullable: true),
                    MaxParentalRatingSubScore = table.Column<int>(type: "integer", nullable: true),
                    RemoteClientBitrateLimit = table.Column<int>(type: "integer", nullable: true),
                    InternalId = table.Column<long>(type: "bigint", nullable: false),
                    SyncPlayAccess = table.Column<int>(type: "integer", nullable: false),
                    CastReceiverId = table.Column<string>(type: "text", maxLength: 32, nullable: true, collation: "C"),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AncestorIds",
                columns: table => new
                {
                    ParentItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "AttachmentStreamInfos",
                columns: table => new
                {
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Index = table.Column<int>(type: "integer", nullable: false),
                    Codec = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    CodecTag = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    Comment = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    Filename = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    MimeType = table.Column<string>(type: "text", nullable: true, collation: "C")
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
                });

            migrationBuilder.CreateTable(
                name: "BaseItemImageInfos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Path = table.Column<string>(type: "text", nullable: false, collation: "C"),
                    DateModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ImageType = table.Column<int>(type: "integer", nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: false),
                    Height = table.Column<int>(type: "integer", nullable: false),
                    Blurhash = table.Column<byte[]>(type: "bytea", nullable: true),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "BaseItemMetadataFields",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "BaseItemProviders",
                columns: table => new
                {
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<string>(type: "text", nullable: false, collation: "C"),
                    ProviderValue = table.Column<string>(type: "text", nullable: false, collation: "C")
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
                });

            migrationBuilder.CreateTable(
                name: "BaseItemTrailerTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "Chapters",
                columns: table => new
                {
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChapterIndex = table.Column<int>(type: "integer", nullable: false),
                    StartPositionTicks = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    ImagePath = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    ImageDateModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                });

            migrationBuilder.CreateTable(
                name: "KeyframeData",
                columns: table => new
                {
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalDuration = table.Column<long>(type: "bigint", nullable: false),
                    KeyframeTicks = table.Column<long[]>(type: "bigint[]", nullable: true)
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
                });

            migrationBuilder.CreateTable(
                name: "LinkedChildren",
                columns: table => new
                {
                    ParentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    ChildId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChildType = table.Column<int>(type: "integer", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "MediaStreamInfos",
                columns: table => new
                {
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    StreamIndex = table.Column<int>(type: "integer", nullable: false),
                    StreamType = table.Column<int>(type: "integer", nullable: false),
                    Codec = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    Language = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    ChannelLayout = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    Profile = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    AspectRatio = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    Path = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    IsInterlaced = table.Column<bool>(type: "boolean", nullable: true),
                    BitRate = table.Column<int>(type: "integer", nullable: true),
                    Channels = table.Column<int>(type: "integer", nullable: true),
                    SampleRate = table.Column<int>(type: "integer", nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsForced = table.Column<bool>(type: "boolean", nullable: false),
                    IsExternal = table.Column<bool>(type: "boolean", nullable: false),
                    IsOriginal = table.Column<bool>(type: "boolean", nullable: false),
                    Height = table.Column<int>(type: "integer", nullable: true),
                    Width = table.Column<int>(type: "integer", nullable: true),
                    AverageFrameRate = table.Column<float>(type: "real", nullable: true),
                    RealFrameRate = table.Column<float>(type: "real", nullable: true),
                    Level = table.Column<float>(type: "real", nullable: true),
                    PixelFormat = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    BitDepth = table.Column<int>(type: "integer", nullable: true),
                    IsAnamorphic = table.Column<bool>(type: "boolean", nullable: true),
                    RefFrames = table.Column<int>(type: "integer", nullable: true),
                    CodecTag = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    Comment = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    NalLengthSize = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    IsAvc = table.Column<bool>(type: "boolean", nullable: true),
                    Title = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    TimeBase = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    CodecTimeBase = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    ColorPrimaries = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    ColorSpace = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    ColorTransfer = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    DvVersionMajor = table.Column<int>(type: "integer", nullable: true),
                    DvVersionMinor = table.Column<int>(type: "integer", nullable: true),
                    DvProfile = table.Column<int>(type: "integer", nullable: true),
                    DvLevel = table.Column<int>(type: "integer", nullable: true),
                    RpuPresentFlag = table.Column<int>(type: "integer", nullable: true),
                    ElPresentFlag = table.Column<int>(type: "integer", nullable: true),
                    BlPresentFlag = table.Column<int>(type: "integer", nullable: true),
                    DvBlSignalCompatibilityId = table.Column<int>(type: "integer", nullable: true),
                    IsHearingImpaired = table.Column<bool>(type: "boolean", nullable: true),
                    Rotation = table.Column<int>(type: "integer", nullable: true),
                    KeyFrames = table.Column<string>(type: "text", nullable: true, collation: "C"),
                    Hdr10PlusPresentFlag = table.Column<bool>(type: "boolean", nullable: true)
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
                });

            migrationBuilder.CreateTable(
                name: "ItemValuesMap",
                columns: table => new
                {
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemValueId = table.Column<Guid>(type: "uuid", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "PeopleBaseItemMap",
                columns: table => new
                {
                    Role = table.Column<string>(type: "text", nullable: false, collation: "C"),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeopleId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: true),
                    ListOrder = table.Column<int>(type: "integer", nullable: true)
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
                });

            migrationBuilder.CreateTable(
                name: "AccessSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    StartHour = table.Column<double>(type: "double precision", nullable: false),
                    EndHour = table.Column<double>(type: "double precision", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessToken = table.Column<string>(type: "text", nullable: false, collation: "C"),
                    AppName = table.Column<string>(type: "text", maxLength: 64, nullable: false, collation: "C"),
                    AppVersion = table.Column<string>(type: "text", maxLength: 32, nullable: false, collation: "C"),
                    DeviceName = table.Column<string>(type: "text", maxLength: 64, nullable: false, collation: "C"),
                    DeviceId = table.Column<string>(type: "text", maxLength: 256, nullable: false, collation: "C"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateLastActivity = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "DisplayPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Client = table.Column<string>(type: "text", maxLength: 32, nullable: false, collation: "C"),
                    ShowSidebar = table.Column<bool>(type: "boolean", nullable: false),
                    ShowBackdrop = table.Column<bool>(type: "boolean", nullable: false),
                    ScrollDirection = table.Column<int>(type: "integer", nullable: false),
                    IndexBy = table.Column<int>(type: "integer", nullable: true),
                    SkipForwardLength = table.Column<int>(type: "integer", nullable: false),
                    SkipBackwardLength = table.Column<int>(type: "integer", nullable: false),
                    ChromecastVersion = table.Column<int>(type: "integer", nullable: false),
                    EnableNextVideoInfoOverlay = table.Column<bool>(type: "boolean", nullable: false),
                    DashboardTheme = table.Column<string>(type: "text", maxLength: 32, nullable: true, collation: "C"),
                    TvHome = table.Column<string>(type: "text", maxLength: 32, nullable: true, collation: "C")
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
                });

            migrationBuilder.CreateTable(
                name: "ImageInfos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Path = table.Column<string>(type: "text", maxLength: 512, nullable: false, collation: "C"),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "ItemDisplayPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Client = table.Column<string>(type: "text", maxLength: 32, nullable: false, collation: "C"),
                    ViewType = table.Column<int>(type: "integer", nullable: false),
                    RememberIndexing = table.Column<bool>(type: "boolean", nullable: false),
                    IndexBy = table.Column<int>(type: "integer", nullable: true),
                    RememberSorting = table.Column<bool>(type: "boolean", nullable: false),
                    SortBy = table.Column<string>(type: "text", maxLength: 64, nullable: false, collation: "C"),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "Preferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<string>(type: "text", maxLength: 65535, nullable: false, collation: "C"),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "UserData",
                columns: table => new
                {
                    CustomDataKey = table.Column<string>(type: "text", nullable: false, collation: "C"),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rating = table.Column<double>(type: "double precision", nullable: true),
                    PlaybackPositionTicks = table.Column<long>(type: "bigint", nullable: false),
                    PlayCount = table.Column<int>(type: "integer", nullable: false),
                    IsFavorite = table.Column<bool>(type: "boolean", nullable: false),
                    LastPlayedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Played = table.Column<bool>(type: "boolean", nullable: false),
                    AudioStreamIndex = table.Column<int>(type: "integer", nullable: true),
                    SubtitleStreamIndex = table.Column<int>(type: "integer", nullable: true),
                    Likes = table.Column<bool>(type: "boolean", nullable: true),
                    RetentionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                });

            migrationBuilder.CreateTable(
                name: "HomeSection",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DisplayPreferencesId = table.Column<int>(type: "integer", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false)
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
                });

            migrationBuilder.InsertData(
                table: "BaseItems",
                columns: new[] { "Id", "Album", "AlbumArtists", "Artists", "Audio", "ChannelId", "CleanName", "CommunityRating", "CriticRating", "CustomRating", "Data", "DateCreated", "DateLastMediaAdded", "DateLastRefreshed", "DateLastSaved", "DateModified", "EndDate", "EpisodeTitle", "ExternalId", "ExternalSeriesId", "ExternalServiceId", "ExtraType", "ForcedSortName", "Genres", "Height", "IndexNumber", "InheritedParentalRatingSubValue", "InheritedParentalRatingValue", "IsFolder", "IsInMixedFolder", "IsLocked", "IsMovie", "IsRepeat", "IsSeries", "IsVirtualItem", "LUFS", "MediaType", "Name", "NormalizationGain", "OfficialRating", "OriginalLanguage", "OriginalTitle", "Overview", "OwnerId", "ParentId", "ParentIndexNumber", "Path", "PreferredMetadataCountryCode", "PreferredMetadataLanguage", "PremiereDate", "PresentationUniqueKey", "PrimaryVersionId", "ProductionLocations", "ProductionYear", "RunTimeTicks", "SeasonId", "SeasonName", "SeriesId", "SeriesName", "SeriesPresentationUniqueKey", "ShowId", "Size", "SortName", "StartDate", "Studios", "Tagline", "Tags", "TopParentId", "TotalBitrate", "Type", "UnratedType", "Width" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, false, false, false, false, false, false, false, null, null, "This is a placeholder item for UserData that has been detached from its original item", null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, "PLACEHOLDER", null, null });

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
                columns: new[] { "ProviderId", "ItemId", "ProviderValue" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_ExtraType_OwnerId",
                table: "BaseItems",
                columns: new[] { "ExtraType", "OwnerId" },
                filter: "\"ExtraType\" IS NOT NULL")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsFirst, NullSortOrder.NullsFirst });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_IsFolder_TopParentId_IsVirtualItem_Presen_d385e7f8",
                table: "BaseItems",
                columns: new[] { "IsFolder", "TopParentId", "IsVirtualItem", "PresentationUniqueKey", "DateCreated" })
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.Unspecified, NullSortOrder.NullsFirst, NullSortOrder.Unspecified, NullSortOrder.NullsFirst, NullSortOrder.NullsFirst });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_MediaType_TopParentId_IsVirtualItem_Prese_20a0002d",
                table: "BaseItems",
                columns: new[] { "MediaType", "TopParentId", "IsVirtualItem", "PresentationUniqueKey" })
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsFirst, NullSortOrder.NullsFirst, NullSortOrder.Unspecified, NullSortOrder.NullsFirst });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Name",
                table: "BaseItems",
                column: "Name")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsFirst });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_OwnerId",
                table: "BaseItems",
                column: "OwnerId",
                filter: "\"OwnerId\" IS NOT NULL")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsFirst });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_ParentId",
                table: "BaseItems",
                column: "ParentId")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsFirst });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Path",
                table: "BaseItems",
                column: "Path")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsFirst });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_PresentationUniqueKey",
                table: "BaseItems",
                column: "PresentationUniqueKey")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsFirst });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_PrimaryVersionId",
                table: "BaseItems",
                column: "PrimaryVersionId",
                filter: "\"PrimaryVersionId\" IS NOT NULL")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsFirst });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_SeasonId",
                table: "BaseItems",
                column: "SeasonId")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsFirst });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_SeriesId",
                table: "BaseItems",
                column: "SeriesId")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsFirst });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_SeriesName",
                table: "BaseItems",
                column: "SeriesName")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsFirst });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_TopParentId_Id",
                table: "BaseItems",
                columns: new[] { "TopParentId", "Id" })
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsFirst, NullSortOrder.Unspecified });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_TopParentId_IsFolder_IsVirtualItem_DateCreated",
                table: "BaseItems",
                columns: new[] { "TopParentId", "IsFolder", "IsVirtualItem", "DateCreated" })
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsFirst, NullSortOrder.Unspecified, NullSortOrder.Unspecified, NullSortOrder.NullsFirst });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_TopParentId_MediaType_IsVirtualItem_DateCreated",
                table: "BaseItems",
                columns: new[] { "TopParentId", "MediaType", "IsVirtualItem", "DateCreated" })
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsFirst, NullSortOrder.NullsFirst, NullSortOrder.Unspecified, NullSortOrder.NullsFirst });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_TopParentId_Type_IsVirtualItem",
                table: "BaseItems",
                columns: new[] { "TopParentId", "Type", "IsVirtualItem" },
                filter: "\"PrimaryVersionId\" IS NULL AND (\"OwnerId\" IS NULL OR \"ExtraType\" IS NOT NULL)")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsFirst, NullSortOrder.Unspecified, NullSortOrder.Unspecified });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_TopParentId_Type_IsVirtualItem_DateCreated",
                table: "BaseItems",
                columns: new[] { "TopParentId", "Type", "IsVirtualItem", "DateCreated" })
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsFirst, NullSortOrder.Unspecified, NullSortOrder.Unspecified, NullSortOrder.NullsFirst });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Type_CleanName",
                table: "BaseItems",
                columns: new[] { "Type", "CleanName" })
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.Unspecified, NullSortOrder.NullsFirst });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Type_SeriesPresentationUniqueKey_IsFolder_8661462a",
                table: "BaseItems",
                columns: new[] { "Type", "SeriesPresentationUniqueKey", "IsFolder", "IsVirtualItem" })
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.Unspecified, NullSortOrder.NullsFirst, NullSortOrder.Unspecified, NullSortOrder.Unspecified });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Type_SeriesPresentationUniqueKey_ParentIn_f161c196",
                table: "BaseItems",
                columns: new[] { "Type", "SeriesPresentationUniqueKey", "ParentIndexNumber", "IndexNumber" })
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.Unspecified, NullSortOrder.NullsFirst, NullSortOrder.NullsFirst, NullSortOrder.NullsFirst });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Type_SeriesPresentationUniqueKey_Presenta_d243c44a",
                table: "BaseItems",
                columns: new[] { "Type", "SeriesPresentationUniqueKey", "PresentationUniqueKey", "SortName" })
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.Unspecified, NullSortOrder.NullsFirst, NullSortOrder.NullsFirst, NullSortOrder.NullsFirst });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Type_TopParentId_Id",
                table: "BaseItems",
                columns: new[] { "Type", "TopParentId", "Id" })
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.Unspecified, NullSortOrder.NullsFirst, NullSortOrder.Unspecified });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Type_TopParentId_IsVirtualItem_Presentati_5c120b7b",
                table: "BaseItems",
                columns: new[] { "Type", "TopParentId", "IsVirtualItem", "PresentationUniqueKey", "DateCreated" })
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.Unspecified, NullSortOrder.NullsFirst, NullSortOrder.Unspecified, NullSortOrder.NullsFirst, NullSortOrder.NullsFirst });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Type_TopParentId_PresentationUniqueKey",
                table: "BaseItems",
                columns: new[] { "Type", "TopParentId", "PresentationUniqueKey" })
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.Unspecified, NullSortOrder.NullsFirst, NullSortOrder.NullsFirst });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Type_TopParentId_SortName",
                table: "BaseItems",
                columns: new[] { "Type", "TopParentId", "SortName" })
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.Unspecified, NullSortOrder.NullsFirst, NullSortOrder.NullsFirst });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Type_TopParentId_StartDate",
                table: "BaseItems",
                columns: new[] { "Type", "TopParentId", "StartDate" })
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.Unspecified, NullSortOrder.NullsFirst, NullSortOrder.NullsFirst });

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
                unique: true)
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsFirst });

            migrationBuilder.CreateIndex(
                name: "IX_ItemDisplayPreferences_UserId",
                table: "ItemDisplayPreferences",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemValues_Type_CleanValue",
                table: "ItemValues",
                columns: new[] { "Type", "CleanValue" });

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
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.Unspecified, NullSortOrder.Unspecified, NullSortOrder.NullsFirst, NullSortOrder.Unspecified });

            migrationBuilder.CreateIndex(
                name: "IX_PeopleBaseItemMap_ItemId_ListOrder",
                table: "PeopleBaseItemMap",
                columns: new[] { "ItemId", "ListOrder" })
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.Unspecified, NullSortOrder.NullsFirst });

            migrationBuilder.CreateIndex(
                name: "IX_PeopleBaseItemMap_ItemId_SortOrder",
                table: "PeopleBaseItemMap",
                columns: new[] { "ItemId", "SortOrder" })
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.Unspecified, NullSortOrder.NullsFirst });

            migrationBuilder.CreateIndex(
                name: "IX_PeopleBaseItemMap_PeopleId_ItemId",
                table: "PeopleBaseItemMap",
                columns: new[] { "PeopleId", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_Peoples_Name",
                table: "Peoples",
                column: "Name");

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
                columns: new[] { "ItemId", "UserId", "LastPlayedDate" })
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.Unspecified, NullSortOrder.Unspecified, NullSortOrder.NullsFirst });

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
                columns: new[] { "UserId", "ItemId", "LastPlayedDate" })
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.Unspecified, NullSortOrder.Unspecified, NullSortOrder.NullsFirst });

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

            CreateHandWrittenObjects(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            DropHandWrittenObjects(migrationBuilder);

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
        }
    }
}
