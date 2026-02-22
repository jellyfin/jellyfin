using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Server.Implementations.Migrations
{
    /// <inheritdoc />
    public partial class LibraryDbMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BaseItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Data = table.Column<string>(type: "TEXT", nullable: true),
                    Path = table.Column<string>(type: "TEXT", nullable: true),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ChannelId = table.Column<string>(type: "TEXT", nullable: true),
                    IsMovie = table.Column<bool>(type: "INTEGER", nullable: false),
                    CommunityRating = table.Column<float>(type: "REAL", nullable: true),
                    CustomRating = table.Column<string>(type: "TEXT", nullable: true),
                    IndexNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    IsLocked = table.Column<bool>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    OfficialRating = table.Column<string>(type: "TEXT", nullable: true),
                    MediaType = table.Column<string>(type: "TEXT", nullable: true),
                    Overview = table.Column<string>(type: "TEXT", nullable: true),
                    ParentIndexNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    PremiereDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ProductionYear = table.Column<int>(type: "INTEGER", nullable: true),
                    Genres = table.Column<string>(type: "TEXT", nullable: true),
                    SortName = table.Column<string>(type: "TEXT", nullable: true),
                    ForcedSortName = table.Column<string>(type: "TEXT", nullable: true),
                    RunTimeTicks = table.Column<long>(type: "INTEGER", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DateModified = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsSeries = table.Column<bool>(type: "INTEGER", nullable: false),
                    EpisodeTitle = table.Column<string>(type: "TEXT", nullable: true),
                    IsRepeat = table.Column<bool>(type: "INTEGER", nullable: false),
                    PreferredMetadataLanguage = table.Column<string>(type: "TEXT", nullable: true),
                    PreferredMetadataCountryCode = table.Column<string>(type: "TEXT", nullable: true),
                    DateLastRefreshed = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DateLastSaved = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsInMixedFolder = table.Column<bool>(type: "INTEGER", nullable: false),
                    Studios = table.Column<string>(type: "TEXT", nullable: true),
                    ExternalServiceId = table.Column<string>(type: "TEXT", nullable: true),
                    Tags = table.Column<string>(type: "TEXT", nullable: true),
                    IsFolder = table.Column<bool>(type: "INTEGER", nullable: false),
                    InheritedParentalRatingValue = table.Column<int>(type: "INTEGER", nullable: true),
                    UnratedType = table.Column<string>(type: "TEXT", nullable: true),
                    CriticRating = table.Column<float>(type: "REAL", nullable: true),
                    CleanName = table.Column<string>(type: "TEXT", nullable: true),
                    PresentationUniqueKey = table.Column<string>(type: "TEXT", nullable: true),
                    OriginalTitle = table.Column<string>(type: "TEXT", nullable: true),
                    PrimaryVersionId = table.Column<string>(type: "TEXT", nullable: true),
                    DateLastMediaAdded = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Album = table.Column<string>(type: "TEXT", nullable: true),
                    LUFS = table.Column<float>(type: "REAL", nullable: true),
                    NormalizationGain = table.Column<float>(type: "REAL", nullable: true),
                    IsVirtualItem = table.Column<bool>(type: "INTEGER", nullable: false),
                    SeriesName = table.Column<string>(type: "TEXT", nullable: true),
                    SeasonName = table.Column<string>(type: "TEXT", nullable: true),
                    ExternalSeriesId = table.Column<string>(type: "TEXT", nullable: true),
                    Tagline = table.Column<string>(type: "TEXT", nullable: true),
                    ProductionLocations = table.Column<string>(type: "TEXT", nullable: true),
                    ExtraIds = table.Column<string>(type: "TEXT", nullable: true),
                    TotalBitrate = table.Column<int>(type: "INTEGER", nullable: true),
                    ExtraType = table.Column<int>(type: "INTEGER", nullable: true),
                    Artists = table.Column<string>(type: "TEXT", nullable: true),
                    AlbumArtists = table.Column<string>(type: "TEXT", nullable: true),
                    ExternalId = table.Column<string>(type: "TEXT", nullable: true),
                    SeriesPresentationUniqueKey = table.Column<string>(type: "TEXT", nullable: true),
                    ShowId = table.Column<string>(type: "TEXT", nullable: true),
                    OwnerId = table.Column<string>(type: "TEXT", nullable: true),
                    Width = table.Column<int>(type: "INTEGER", nullable: true),
                    Height = table.Column<int>(type: "INTEGER", nullable: true),
                    Size = table.Column<long>(type: "INTEGER", nullable: true),
                    Audio = table.Column<int>(type: "INTEGER", nullable: true),
                    ParentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TopParentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SeasonId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SeriesId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaseItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItemValues",
                columns: table => new
                {
                    ItemValueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    CleanValue = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemValues", x => x.ItemValueId);
                });

            migrationBuilder.CreateTable(
                name: "Peoples",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    PersonType = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Peoples", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AncestorIds",
                columns: table => new
                {
                    ParentItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BaseItemEntityId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AncestorIds", x => new { x.ItemId, x.ParentItemId });
                    table.ForeignKey(
                        name: "FK_AncestorIds_BaseItems_BaseItemEntityId",
                        column: x => x.BaseItemEntityId,
                        principalTable: "BaseItems",
                        principalColumn: "Id");
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
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Index = table.Column<int>(type: "INTEGER", nullable: false),
                    Codec = table.Column<string>(type: "TEXT", nullable: false),
                    CodecTag = table.Column<string>(type: "TEXT", nullable: true),
                    Comment = table.Column<string>(type: "TEXT", nullable: true),
                    Filename = table.Column<string>(type: "TEXT", nullable: true),
                    MimeType = table.Column<string>(type: "TEXT", nullable: true)
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
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Path = table.Column<string>(type: "TEXT", nullable: false),
                    DateModified = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ImageType = table.Column<int>(type: "INTEGER", nullable: false),
                    Width = table.Column<int>(type: "INTEGER", nullable: false),
                    Height = table.Column<int>(type: "INTEGER", nullable: false),
                    Blurhash = table.Column<byte[]>(type: "BLOB", nullable: true),
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false)
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
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false)
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
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProviderId = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderValue = table.Column<string>(type: "TEXT", nullable: false)
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
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false)
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
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChapterIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    StartPositionTicks = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    ImagePath = table.Column<string>(type: "TEXT", nullable: true),
                    ImageDateModified = table.Column<DateTime>(type: "TEXT", nullable: true)
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
                name: "MediaStreamInfos",
                columns: table => new
                {
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StreamIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    StreamType = table.Column<int>(type: "INTEGER", nullable: true),
                    Codec = table.Column<string>(type: "TEXT", nullable: true),
                    Language = table.Column<string>(type: "TEXT", nullable: true),
                    ChannelLayout = table.Column<string>(type: "TEXT", nullable: true),
                    Profile = table.Column<string>(type: "TEXT", nullable: true),
                    AspectRatio = table.Column<string>(type: "TEXT", nullable: true),
                    Path = table.Column<string>(type: "TEXT", nullable: true),
                    IsInterlaced = table.Column<bool>(type: "INTEGER", nullable: false),
                    BitRate = table.Column<int>(type: "INTEGER", nullable: false),
                    Channels = table.Column<int>(type: "INTEGER", nullable: false),
                    SampleRate = table.Column<int>(type: "INTEGER", nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsForced = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsExternal = table.Column<bool>(type: "INTEGER", nullable: false),
                    Height = table.Column<int>(type: "INTEGER", nullable: false),
                    Width = table.Column<int>(type: "INTEGER", nullable: false),
                    AverageFrameRate = table.Column<float>(type: "REAL", nullable: false),
                    RealFrameRate = table.Column<float>(type: "REAL", nullable: false),
                    Level = table.Column<float>(type: "REAL", nullable: false),
                    PixelFormat = table.Column<string>(type: "TEXT", nullable: true),
                    BitDepth = table.Column<int>(type: "INTEGER", nullable: false),
                    IsAnamorphic = table.Column<bool>(type: "INTEGER", nullable: false),
                    RefFrames = table.Column<int>(type: "INTEGER", nullable: false),
                    CodecTag = table.Column<string>(type: "TEXT", nullable: false),
                    Comment = table.Column<string>(type: "TEXT", nullable: false),
                    NalLengthSize = table.Column<string>(type: "TEXT", nullable: false),
                    IsAvc = table.Column<bool>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    TimeBase = table.Column<string>(type: "TEXT", nullable: false),
                    CodecTimeBase = table.Column<string>(type: "TEXT", nullable: false),
                    ColorPrimaries = table.Column<string>(type: "TEXT", nullable: false),
                    ColorSpace = table.Column<string>(type: "TEXT", nullable: false),
                    ColorTransfer = table.Column<string>(type: "TEXT", nullable: false),
                    DvVersionMajor = table.Column<int>(type: "INTEGER", nullable: false),
                    DvVersionMinor = table.Column<int>(type: "INTEGER", nullable: false),
                    DvProfile = table.Column<int>(type: "INTEGER", nullable: false),
                    DvLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    RpuPresentFlag = table.Column<int>(type: "INTEGER", nullable: false),
                    ElPresentFlag = table.Column<int>(type: "INTEGER", nullable: false),
                    BlPresentFlag = table.Column<int>(type: "INTEGER", nullable: false),
                    DvBlSignalCompatibilityId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsHearingImpaired = table.Column<bool>(type: "INTEGER", nullable: false),
                    Rotation = table.Column<int>(type: "INTEGER", nullable: false),
                    KeyFrames = table.Column<string>(type: "TEXT", nullable: true)
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
                name: "UserData",
                columns: table => new
                {
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Rating = table.Column<double>(type: "REAL", nullable: true),
                    PlaybackPositionTicks = table.Column<long>(type: "INTEGER", nullable: false),
                    PlayCount = table.Column<int>(type: "INTEGER", nullable: false),
                    IsFavorite = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastPlayedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Played = table.Column<bool>(type: "INTEGER", nullable: false),
                    AudioStreamIndex = table.Column<int>(type: "INTEGER", nullable: true),
                    SubtitleStreamIndex = table.Column<int>(type: "INTEGER", nullable: true),
                    Likes = table.Column<bool>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserData", x => new { x.ItemId, x.UserId });
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
                name: "ItemValuesMap",
                columns: table => new
                {
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ItemValueId = table.Column<Guid>(type: "TEXT", nullable: false)
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
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PeopleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: true),
                    ListOrder = table.Column<int>(type: "INTEGER", nullable: true),
                    Role = table.Column<string>(type: "TEXT", nullable: true)
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

            migrationBuilder.CreateIndex(
                name: "IX_AncestorIds_BaseItemEntityId",
                table: "AncestorIds",
                column: "BaseItemEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_AncestorIds_ParentItemId",
                table: "AncestorIds",
                column: "ParentItemId");

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
                name: "IX_ItemValues_Type_CleanValue",
                table: "ItemValues",
                columns: new[] { "Type", "CleanValue" });

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AncestorIds");

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
                name: "ItemValuesMap");

            migrationBuilder.DropTable(
                name: "MediaStreamInfos");

            migrationBuilder.DropTable(
                name: "PeopleBaseItemMap");

            migrationBuilder.DropTable(
                name: "UserData");

            migrationBuilder.DropTable(
                name: "ItemValues");

            migrationBuilder.DropTable(
                name: "Peoples");

            migrationBuilder.DropTable(
                name: "BaseItems");
        }
    }
}
