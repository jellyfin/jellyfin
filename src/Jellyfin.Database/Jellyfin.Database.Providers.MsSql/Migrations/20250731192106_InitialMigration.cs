using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Database.Providers.MsSql.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivityLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Overview = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ShortOverview = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LogSeverity = table.Column<int>(type: "int", nullable: false),
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
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateLastActivity = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AccessToken = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BaseItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Data = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Path = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ChannelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsMovie = table.Column<bool>(type: "bit", nullable: false),
                    CommunityRating = table.Column<float>(type: "real", nullable: true),
                    CustomRating = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IndexNumber = table.Column<int>(type: "int", nullable: true),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OfficialRating = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MediaType = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Overview = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ParentIndexNumber = table.Column<int>(type: "int", nullable: true),
                    PremiereDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProductionYear = table.Column<int>(type: "int", nullable: true),
                    Genres = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortName = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ForcedSortName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RunTimeTicks = table.Column<long>(type: "bigint", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsSeries = table.Column<bool>(type: "bit", nullable: false),
                    EpisodeTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRepeat = table.Column<bool>(type: "bit", nullable: false),
                    PreferredMetadataLanguage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PreferredMetadataCountryCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateLastRefreshed = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateLastSaved = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsInMixedFolder = table.Column<bool>(type: "bit", nullable: false),
                    Studios = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExternalServiceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsFolder = table.Column<bool>(type: "bit", nullable: false),
                    InheritedParentalRatingValue = table.Column<int>(type: "int", nullable: true),
                    InheritedParentalRatingSubValue = table.Column<int>(type: "int", nullable: true),
                    UnratedType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CriticRating = table.Column<float>(type: "real", nullable: true),
                    CleanName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PresentationUniqueKey = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    OriginalTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrimaryVersionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateLastMediaAdded = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Album = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LUFS = table.Column<float>(type: "real", nullable: true),
                    NormalizationGain = table.Column<float>(type: "real", nullable: true),
                    IsVirtualItem = table.Column<bool>(type: "bit", nullable: false),
                    SeriesName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SeasonName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExternalSeriesId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tagline = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProductionLocations = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExtraIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalBitrate = table.Column<int>(type: "int", nullable: true),
                    ExtraType = table.Column<int>(type: "int", nullable: true),
                    Artists = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AlbumArtists = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExternalId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SeriesPresentationUniqueKey = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ShowId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OwnerId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Width = table.Column<int>(type: "int", nullable: true),
                    Height = table.Column<int>(type: "int", nullable: true),
                    Size = table.Column<long>(type: "bigint", nullable: true),
                    Audio = table.Column<int>(type: "int", nullable: true),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TopParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SeasonId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SeriesId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaseItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomItemDisplayPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Client = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Key = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomItemDisplayPreferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeviceOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CustomName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceOptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItemValues",
                columns: table => new
                {
                    ItemValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CleanValue = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemValues", x => x.ItemValueId);
                });

            migrationBuilder.CreateTable(
                name: "MediaSegments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    EndTicks = table.Column<long>(type: "bigint", nullable: false),
                    StartTicks = table.Column<long>(type: "bigint", nullable: false),
                    SegmentProviderId = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaSegments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Peoples",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PersonType = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Peoples", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrickplayInfos",
                columns: table => new
                {
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", maxLength: 65535, nullable: true),
                    MustUpdatePassword = table.Column<bool>(type: "bit", nullable: false),
                    AudioLanguagePreference = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    AuthenticationProviderId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PasswordResetProviderId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    InvalidLoginAttemptCount = table.Column<int>(type: "int", nullable: false),
                    LastActivityDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastLoginDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LoginAttemptsBeforeLockout = table.Column<int>(type: "int", nullable: true),
                    MaxActiveSessions = table.Column<int>(type: "int", nullable: false),
                    SubtitleMode = table.Column<int>(type: "int", nullable: false),
                    PlayDefaultAudioTrack = table.Column<bool>(type: "bit", nullable: false),
                    SubtitleLanguagePreference = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DisplayMissingEpisodes = table.Column<bool>(type: "bit", nullable: false),
                    DisplayCollectionsView = table.Column<bool>(type: "bit", nullable: false),
                    EnableLocalPassword = table.Column<bool>(type: "bit", nullable: false),
                    HidePlayedInLatest = table.Column<bool>(type: "bit", nullable: false),
                    RememberAudioSelections = table.Column<bool>(type: "bit", nullable: false),
                    RememberSubtitleSelections = table.Column<bool>(type: "bit", nullable: false),
                    EnableNextEpisodeAutoPlay = table.Column<bool>(type: "bit", nullable: false),
                    EnableAutoLogin = table.Column<bool>(type: "bit", nullable: false),
                    EnableUserPreferenceAccess = table.Column<bool>(type: "bit", nullable: false),
                    MaxParentalRatingScore = table.Column<int>(type: "int", nullable: true),
                    MaxParentalRatingSubScore = table.Column<int>(type: "int", nullable: true),
                    RemoteClientBitrateLimit = table.Column<int>(type: "int", nullable: true),
                    InternalId = table.Column<long>(type: "bigint", nullable: false),
                    SyncPlayAccess = table.Column<int>(type: "int", nullable: false),
                    CastReceiverId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
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
                    ParentItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "AttachmentStreamInfos",
                columns: table => new
                {
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Index = table.Column<int>(type: "int", nullable: false),
                    Codec = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodecTag = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Filename = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MimeType = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Path = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ImageType = table.Column<int>(type: "int", nullable: false),
                    Width = table.Column<int>(type: "int", nullable: false),
                    Height = table.Column<int>(type: "int", nullable: false),
                    Blurhash = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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
                    Id = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderValue = table.Column<string>(type: "nvarchar(450)", nullable: false)
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
                    Id = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChapterIndex = table.Column<int>(type: "int", nullable: false),
                    StartPositionTicks = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImagePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImageDateModified = table.Column<DateTime>(type: "datetime2", nullable: true)
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
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalDuration = table.Column<long>(type: "bigint", nullable: false),
                    KeyframeTicks = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                name: "MediaStreamInfos",
                columns: table => new
                {
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StreamIndex = table.Column<int>(type: "int", nullable: false),
                    StreamType = table.Column<int>(type: "int", nullable: false),
                    Codec = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Language = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ChannelLayout = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Profile = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AspectRatio = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Path = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsInterlaced = table.Column<bool>(type: "bit", nullable: true),
                    BitRate = table.Column<int>(type: "int", nullable: true),
                    Channels = table.Column<int>(type: "int", nullable: true),
                    SampleRate = table.Column<int>(type: "int", nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsForced = table.Column<bool>(type: "bit", nullable: false),
                    IsExternal = table.Column<bool>(type: "bit", nullable: false),
                    Height = table.Column<int>(type: "int", nullable: true),
                    Width = table.Column<int>(type: "int", nullable: true),
                    AverageFrameRate = table.Column<float>(type: "real", nullable: true),
                    RealFrameRate = table.Column<float>(type: "real", nullable: true),
                    Level = table.Column<float>(type: "real", nullable: true),
                    PixelFormat = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BitDepth = table.Column<int>(type: "int", nullable: true),
                    IsAnamorphic = table.Column<bool>(type: "bit", nullable: true),
                    RefFrames = table.Column<int>(type: "int", nullable: true),
                    CodecTag = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NalLengthSize = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsAvc = table.Column<bool>(type: "bit", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TimeBase = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodecTimeBase = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ColorPrimaries = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ColorSpace = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ColorTransfer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DvVersionMajor = table.Column<int>(type: "int", nullable: true),
                    DvVersionMinor = table.Column<int>(type: "int", nullable: true),
                    DvProfile = table.Column<int>(type: "int", nullable: true),
                    DvLevel = table.Column<int>(type: "int", nullable: true),
                    RpuPresentFlag = table.Column<int>(type: "int", nullable: true),
                    ElPresentFlag = table.Column<int>(type: "int", nullable: true),
                    BlPresentFlag = table.Column<int>(type: "int", nullable: true),
                    DvBlSignalCompatibilityId = table.Column<int>(type: "int", nullable: true),
                    IsHearingImpaired = table.Column<bool>(type: "bit", nullable: true),
                    Rotation = table.Column<int>(type: "int", nullable: true),
                    KeyFrames = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Hdr10PlusPresentFlag = table.Column<bool>(type: "bit", nullable: true)
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
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeopleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: true),
                    ListOrder = table.Column<int>(type: "int", nullable: true),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeopleBaseItemMap", x => new { x.ItemId, x.PeopleId });
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
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    StartHour = table.Column<double>(type: "float", nullable: false),
                    EndHour = table.Column<double>(type: "float", nullable: false)
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
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccessToken = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AppName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AppVersion = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DeviceName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateLastActivity = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Client = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ShowSidebar = table.Column<bool>(type: "bit", nullable: false),
                    ShowBackdrop = table.Column<bool>(type: "bit", nullable: false),
                    ScrollDirection = table.Column<int>(type: "int", nullable: false),
                    IndexBy = table.Column<int>(type: "int", nullable: true),
                    SkipForwardLength = table.Column<int>(type: "int", nullable: false),
                    SkipBackwardLength = table.Column<int>(type: "int", nullable: false),
                    ChromecastVersion = table.Column<int>(type: "int", nullable: false),
                    EnableNextVideoInfoOverlay = table.Column<bool>(type: "bit", nullable: false),
                    DashboardTheme = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    TvHome = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true)
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
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Path = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Client = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ViewType = table.Column<int>(type: "int", nullable: false),
                    RememberIndexing = table.Column<bool>(type: "bit", nullable: false),
                    IndexBy = table.Column<int>(type: "int", nullable: true),
                    RememberSorting = table.Column<bool>(type: "bit", nullable: false),
                    SortBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
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
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    Permission_Permissions_Guid = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
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
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", maxLength: 65535, nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    Preference_Preferences_Guid = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
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
                    CustomDataKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rating = table.Column<double>(type: "float", nullable: true),
                    PlaybackPositionTicks = table.Column<long>(type: "bigint", nullable: false),
                    PlayCount = table.Column<int>(type: "int", nullable: false),
                    IsFavorite = table.Column<bool>(type: "bit", nullable: false),
                    LastPlayedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Played = table.Column<bool>(type: "bit", nullable: false),
                    AudioStreamIndex = table.Column<int>(type: "int", nullable: true),
                    SubtitleStreamIndex = table.Column<int>(type: "int", nullable: true),
                    Likes = table.Column<bool>(type: "bit", nullable: true),
                    RetentionDate = table.Column<DateTime>(type: "datetime2", nullable: true)
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
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
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
                });

            migrationBuilder.InsertData(
                table: "BaseItems",
                columns: new[] { "Id", "Album", "AlbumArtists", "Artists", "Audio", "ChannelId", "CleanName", "CommunityRating", "CriticRating", "CustomRating", "Data", "DateCreated", "DateLastMediaAdded", "DateLastRefreshed", "DateLastSaved", "DateModified", "EndDate", "EpisodeTitle", "ExternalId", "ExternalSeriesId", "ExternalServiceId", "ExtraIds", "ExtraType", "ForcedSortName", "Genres", "Height", "IndexNumber", "InheritedParentalRatingSubValue", "InheritedParentalRatingValue", "IsFolder", "IsInMixedFolder", "IsLocked", "IsMovie", "IsRepeat", "IsSeries", "IsVirtualItem", "LUFS", "MediaType", "Name", "NormalizationGain", "OfficialRating", "OriginalTitle", "Overview", "OwnerId", "ParentId", "ParentIndexNumber", "Path", "PreferredMetadataCountryCode", "PreferredMetadataLanguage", "PremiereDate", "PresentationUniqueKey", "PrimaryVersionId", "ProductionLocations", "ProductionYear", "RunTimeTicks", "SeasonId", "SeasonName", "SeriesId", "SeriesName", "SeriesPresentationUniqueKey", "ShowId", "Size", "SortName", "StartDate", "Studios", "Tagline", "Tags", "TopParentId", "TotalBitrate", "Type", "UnratedType", "Width" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, false, false, false, false, false, false, false, null, null, "This is a placeholder item for UserData that has been detacted from its original item", null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, "PLACEHOLDER", null, null });

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
                name: "IX_BaseItemImageInfos_ItemId",
                table: "BaseItemImageInfos",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_BaseItemMetadataFields_ItemId",
                table: "BaseItemMetadataFields",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_BaseItemProviders_ProviderId_ProviderValue_ItemId",
                table: "BaseItemProviders",
                columns: new[] { "ProviderId", "ProviderValue", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Id_Type_IsFolder_IsVirtualItem",
                table: "BaseItems",
                columns: new[] { "Id", "Type", "IsFolder", "IsVirtualItem" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_IsFolder_TopParentId_IsVirtualItem_PresentationUniqueKey_DateCreated",
                table: "BaseItems",
                columns: new[] { "IsFolder", "TopParentId", "IsVirtualItem", "PresentationUniqueKey", "DateCreated" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_MediaType_TopParentId_IsVirtualItem_PresentationUniqueKey",
                table: "BaseItems",
                columns: new[] { "MediaType", "TopParentId", "IsVirtualItem", "PresentationUniqueKey" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_ParentId",
                table: "BaseItems",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Path",
                table: "BaseItems",
                column: "Path");

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_PresentationUniqueKey",
                table: "BaseItems",
                column: "PresentationUniqueKey");

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_TopParentId_Id",
                table: "BaseItems",
                columns: new[] { "TopParentId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Type_SeriesPresentationUniqueKey_IsFolder_IsVirtualItem",
                table: "BaseItems",
                columns: new[] { "Type", "SeriesPresentationUniqueKey", "IsFolder", "IsVirtualItem" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Type_SeriesPresentationUniqueKey_PresentationUniqueKey_SortName",
                table: "BaseItems",
                columns: new[] { "Type", "SeriesPresentationUniqueKey", "PresentationUniqueKey", "SortName" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Type_TopParentId_Id",
                table: "BaseItems",
                columns: new[] { "Type", "TopParentId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Type_TopParentId_IsVirtualItem_PresentationUniqueKey_DateCreated",
                table: "BaseItems",
                columns: new[] { "Type", "TopParentId", "IsVirtualItem", "PresentationUniqueKey", "DateCreated" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Type_TopParentId_PresentationUniqueKey",
                table: "BaseItems",
                columns: new[] { "Type", "TopParentId", "PresentationUniqueKey" });

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
                name: "IX_Devices_DeviceId",
                table: "Devices",
                column: "DeviceId");

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
                unique: true,
                filter: "[UserId] IS NOT NULL");

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
                name: "IX_MediaStreamInfos_StreamIndex",
                table: "MediaStreamInfos",
                column: "StreamIndex");

            migrationBuilder.CreateIndex(
                name: "IX_MediaStreamInfos_StreamIndex_StreamType",
                table: "MediaStreamInfos",
                columns: new[] { "StreamIndex", "StreamType" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaStreamInfos_StreamIndex_StreamType_Language",
                table: "MediaStreamInfos",
                columns: new[] { "StreamIndex", "StreamType", "Language" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaStreamInfos_StreamType",
                table: "MediaStreamInfos",
                column: "StreamType");

            migrationBuilder.CreateIndex(
                name: "IX_PeopleBaseItemMap_ItemId_ListOrder",
                table: "PeopleBaseItemMap",
                columns: new[] { "ItemId", "ListOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_PeopleBaseItemMap_ItemId_SortOrder",
                table: "PeopleBaseItemMap",
                columns: new[] { "ItemId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_PeopleBaseItemMap_PeopleId",
                table: "PeopleBaseItemMap",
                column: "PeopleId");

            migrationBuilder.CreateIndex(
                name: "IX_Peoples_Name",
                table: "Peoples",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_UserId_Kind",
                table: "Permissions",
                columns: new[] { "UserId", "Kind" },
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Preferences_UserId_Kind",
                table: "Preferences",
                columns: new[] { "UserId", "Kind" },
                unique: true,
                filter: "[UserId] IS NOT NULL");

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
                name: "IX_UserData_UserId",
                table: "UserData",
                column: "UserId");

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
