#nullable disable

#pragma warning disable CS1591

using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using Emby.Server.Implementations.Playlists;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using Jellyfin.Extensions.Json;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Logging;
using SQLitePCL.pretty;

namespace Emby.Server.Implementations.Data
{
    /// <summary>
    /// Class SQLiteItemRepository.
    /// </summary>
    public class SqliteItemRepository : BaseSqliteRepository, IItemRepository
    {
        private const string FromText = " from TypedBaseItems A";
        private const string ChaptersTableName = "Chapters2";

        private const string SaveItemCommandText =
            @"replace into TypedBaseItems
            (guid,type,data,Path,StartDate,EndDate,ChannelId,IsMovie,IsSeries,EpisodeTitle,IsRepeat,CommunityRating,CustomRating,IndexNumber,IsLocked,Name,OfficialRating,MediaType,Overview,ParentIndexNumber,PremiereDate,ProductionYear,ParentId,Genres,InheritedParentalRatingValue,SortName,ForcedSortName,RunTimeTicks,Size,DateCreated,DateModified,PreferredMetadataLanguage,PreferredMetadataCountryCode,Width,Height,DateLastRefreshed,DateLastSaved,IsInMixedFolder,LockedFields,Studios,Audio,ExternalServiceId,Tags,IsFolder,UnratedType,TopParentId,TrailerTypes,CriticRating,CleanName,PresentationUniqueKey,OriginalTitle,PrimaryVersionId,DateLastMediaAdded,Album,IsVirtualItem,SeriesName,UserDataKey,SeasonName,SeasonId,SeriesId,ExternalSeriesId,Tagline,ProviderIds,Images,ProductionLocations,ExtraIds,TotalBitrate,ExtraType,Artists,AlbumArtists,ExternalId,SeriesPresentationUniqueKey,ShowId,OwnerId)
            values (@guid,@type,@data,@Path,@StartDate,@EndDate,@ChannelId,@IsMovie,@IsSeries,@EpisodeTitle,@IsRepeat,@CommunityRating,@CustomRating,@IndexNumber,@IsLocked,@Name,@OfficialRating,@MediaType,@Overview,@ParentIndexNumber,@PremiereDate,@ProductionYear,@ParentId,@Genres,@InheritedParentalRatingValue,@SortName,@ForcedSortName,@RunTimeTicks,@Size,@DateCreated,@DateModified,@PreferredMetadataLanguage,@PreferredMetadataCountryCode,@Width,@Height,@DateLastRefreshed,@DateLastSaved,@IsInMixedFolder,@LockedFields,@Studios,@Audio,@ExternalServiceId,@Tags,@IsFolder,@UnratedType,@TopParentId,@TrailerTypes,@CriticRating,@CleanName,@PresentationUniqueKey,@OriginalTitle,@PrimaryVersionId,@DateLastMediaAdded,@Album,@IsVirtualItem,@SeriesName,@UserDataKey,@SeasonName,@SeasonId,@SeriesId,@ExternalSeriesId,@Tagline,@ProviderIds,@Images,@ProductionLocations,@ExtraIds,@TotalBitrate,@ExtraType,@Artists,@AlbumArtists,@ExternalId,@SeriesPresentationUniqueKey,@ShowId,@OwnerId)";

        private readonly IServerConfigurationManager _config;
        private readonly IServerApplicationHost _appHost;
        private readonly ILocalizationManager _localization;
        // TODO: Remove this dependency. GetImageCacheTag() is the only method used and it can be converted to a static helper method
        private readonly IImageProcessor _imageProcessor;

        private readonly TypeMapper _typeMapper;
        private readonly JsonSerializerOptions _jsonOptions;

        private readonly ItemFields[] _allItemFields = Enum.GetValues<ItemFields>();

        private static readonly string[] _retrieveItemColumns =
        {
            "type",
            "data",
            "StartDate",
            "EndDate",
            "ChannelId",
            "IsMovie",
            "IsSeries",
            "EpisodeTitle",
            "IsRepeat",
            "CommunityRating",
            "CustomRating",
            "IndexNumber",
            "IsLocked",
            "PreferredMetadataLanguage",
            "PreferredMetadataCountryCode",
            "Width",
            "Height",
            "DateLastRefreshed",
            "Name",
            "Path",
            "PremiereDate",
            "Overview",
            "ParentIndexNumber",
            "ProductionYear",
            "OfficialRating",
            "ForcedSortName",
            "RunTimeTicks",
            "Size",
            "DateCreated",
            "DateModified",
            "guid",
            "Genres",
            "ParentId",
            "Audio",
            "ExternalServiceId",
            "IsInMixedFolder",
            "DateLastSaved",
            "LockedFields",
            "Studios",
            "Tags",
            "TrailerTypes",
            "OriginalTitle",
            "PrimaryVersionId",
            "DateLastMediaAdded",
            "Album",
            "CriticRating",
            "IsVirtualItem",
            "SeriesName",
            "SeasonName",
            "SeasonId",
            "SeriesId",
            "PresentationUniqueKey",
            "InheritedParentalRatingValue",
            "ExternalSeriesId",
            "Tagline",
            "ProviderIds",
            "Images",
            "ProductionLocations",
            "ExtraIds",
            "TotalBitrate",
            "ExtraType",
            "Artists",
            "AlbumArtists",
            "ExternalId",
            "SeriesPresentationUniqueKey",
            "ShowId",
            "OwnerId"
        };

        private static readonly string _retrieveItemColumnsSelectQuery = $"select {string.Join(',', _retrieveItemColumns)} from TypedBaseItems where guid = @guid";

        private static readonly string[] _mediaStreamSaveColumns =
        {
            "ItemId",
            "StreamIndex",
            "StreamType",
            "Codec",
            "Language",
            "ChannelLayout",
            "Profile",
            "AspectRatio",
            "Path",
            "IsInterlaced",
            "BitRate",
            "Channels",
            "SampleRate",
            "IsDefault",
            "IsForced",
            "IsExternal",
            "Height",
            "Width",
            "AverageFrameRate",
            "RealFrameRate",
            "Level",
            "PixelFormat",
            "BitDepth",
            "IsAnamorphic",
            "RefFrames",
            "CodecTag",
            "Comment",
            "NalLengthSize",
            "IsAvc",
            "Title",
            "TimeBase",
            "CodecTimeBase",
            "ColorPrimaries",
            "ColorSpace",
            "ColorTransfer",
            "DvVersionMajor",
            "DvVersionMinor",
            "DvProfile",
            "DvLevel",
            "RpuPresentFlag",
            "ElPresentFlag",
            "BlPresentFlag",
            "DvBlSignalCompatibilityId",
            "IsHearingImpaired"
        };

        private static readonly string _mediaStreamSaveColumnsInsertQuery =
            $"insert into mediastreams ({string.Join(',', _mediaStreamSaveColumns)}) values ";

        private static readonly string _mediaStreamSaveColumnsSelectQuery =
            $"select {string.Join(',', _mediaStreamSaveColumns)} from mediastreams where ItemId=@ItemId";

        private static readonly string[] _mediaAttachmentSaveColumns =
        {
            "ItemId",
            "AttachmentIndex",
            "Codec",
            "CodecTag",
            "Comment",
            "Filename",
            "MIMEType"
        };

        private static readonly string _mediaAttachmentSaveColumnsSelectQuery =
            $"select {string.Join(',', _mediaAttachmentSaveColumns)} from mediaattachments where ItemId=@ItemId";

        private static readonly string _mediaAttachmentInsertPrefix;

        private static readonly BaseItemKind[] _programTypes = new[]
        {
            BaseItemKind.Program,
            BaseItemKind.TvChannel,
            BaseItemKind.LiveTvProgram,
            BaseItemKind.LiveTvChannel
        };

        private static readonly BaseItemKind[] _programExcludeParentTypes = new[]
        {
            BaseItemKind.Series,
            BaseItemKind.Season,
            BaseItemKind.MusicAlbum,
            BaseItemKind.MusicArtist,
            BaseItemKind.PhotoAlbum
        };

        private static readonly BaseItemKind[] _serviceTypes = new[]
        {
            BaseItemKind.TvChannel,
            BaseItemKind.LiveTvChannel
        };

        private static readonly BaseItemKind[] _startDateTypes = new[]
        {
            BaseItemKind.Program,
            BaseItemKind.LiveTvProgram
        };

        private static readonly BaseItemKind[] _seriesTypes = new[]
        {
            BaseItemKind.Book,
            BaseItemKind.AudioBook,
            BaseItemKind.Episode,
            BaseItemKind.Season
        };

        private static readonly BaseItemKind[] _artistExcludeParentTypes = new[]
        {
            BaseItemKind.Series,
            BaseItemKind.Season,
            BaseItemKind.PhotoAlbum
        };

        private static readonly BaseItemKind[] _artistsTypes = new[]
        {
            BaseItemKind.Audio,
            BaseItemKind.MusicAlbum,
            BaseItemKind.MusicVideo,
            BaseItemKind.AudioBook
        };

        private static readonly Dictionary<BaseItemKind, string> _baseItemKindNames = new()
        {
            { BaseItemKind.AggregateFolder, typeof(AggregateFolder).FullName },
            { BaseItemKind.Audio, typeof(Audio).FullName },
            { BaseItemKind.AudioBook, typeof(AudioBook).FullName },
            { BaseItemKind.BasePluginFolder, typeof(BasePluginFolder).FullName },
            { BaseItemKind.Book, typeof(Book).FullName },
            { BaseItemKind.BoxSet, typeof(BoxSet).FullName },
            { BaseItemKind.Channel, typeof(Channel).FullName },
            { BaseItemKind.CollectionFolder, typeof(CollectionFolder).FullName },
            { BaseItemKind.Episode, typeof(Episode).FullName },
            { BaseItemKind.Folder, typeof(Folder).FullName },
            { BaseItemKind.Genre, typeof(Genre).FullName },
            { BaseItemKind.Movie, typeof(Movie).FullName },
            { BaseItemKind.LiveTvChannel, typeof(LiveTvChannel).FullName },
            { BaseItemKind.LiveTvProgram, typeof(LiveTvProgram).FullName },
            { BaseItemKind.MusicAlbum, typeof(MusicAlbum).FullName },
            { BaseItemKind.MusicArtist, typeof(MusicArtist).FullName },
            { BaseItemKind.MusicGenre, typeof(MusicGenre).FullName },
            { BaseItemKind.MusicVideo, typeof(MusicVideo).FullName },
            { BaseItemKind.Person, typeof(Person).FullName },
            { BaseItemKind.Photo, typeof(Photo).FullName },
            { BaseItemKind.PhotoAlbum, typeof(PhotoAlbum).FullName },
            { BaseItemKind.Playlist, typeof(Playlist).FullName },
            { BaseItemKind.PlaylistsFolder, typeof(PlaylistsFolder).FullName },
            { BaseItemKind.Season, typeof(Season).FullName },
            { BaseItemKind.Series, typeof(Series).FullName },
            { BaseItemKind.Studio, typeof(Studio).FullName },
            { BaseItemKind.Trailer, typeof(Trailer).FullName },
            { BaseItemKind.TvChannel, typeof(LiveTvChannel).FullName },
            { BaseItemKind.TvProgram, typeof(LiveTvProgram).FullName },
            { BaseItemKind.UserRootFolder, typeof(UserRootFolder).FullName },
            { BaseItemKind.UserView, typeof(UserView).FullName },
            { BaseItemKind.Video, typeof(Video).FullName },
            { BaseItemKind.Year, typeof(Year).FullName }
        };

        static SqliteItemRepository()
        {
            var queryPrefixText = new StringBuilder();
            queryPrefixText.Append("insert into mediaattachments (");
            foreach (var column in _mediaAttachmentSaveColumns)
            {
                queryPrefixText.Append(column)
                    .Append(',');
            }

            queryPrefixText.Length -= 1;
            queryPrefixText.Append(") values ");
            _mediaAttachmentInsertPrefix = queryPrefixText.ToString();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteItemRepository"/> class.
        /// </summary>
        /// <param name="config">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        /// <param name="appHost">Instance of the <see cref="IServerApplicationHost"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{SqliteItemRepository}"/> interface.</param>
        /// <param name="localization">Instance of the <see cref="ILocalizationManager"/> interface.</param>
        /// <param name="imageProcessor">Instance of the <see cref="IImageProcessor"/> interface.</param>
        /// <exception cref="ArgumentNullException">config is null.</exception>
        public SqliteItemRepository(
            IServerConfigurationManager config,
            IServerApplicationHost appHost,
            ILogger<SqliteItemRepository> logger,
            ILocalizationManager localization,
            IImageProcessor imageProcessor)
            : base(logger)
        {
            _config = config;
            _appHost = appHost;
            _localization = localization;
            _imageProcessor = imageProcessor;

            _typeMapper = new TypeMapper();
            _jsonOptions = JsonDefaults.Options;

            DbFilePath = Path.Combine(_config.ApplicationPaths.DataPath, "library.db");
        }

        /// <inheritdoc />
        protected override int? CacheSize => 20000;

        /// <inheritdoc />
        protected override TempStoreMode TempStore => TempStoreMode.Memory;

        /// <summary>
        /// Opens the connection to the database.
        /// </summary>
        /// <param name="userDataRepo">The user data repository.</param>
        /// <param name="userManager">The user manager.</param>
        public void Initialize(SqliteUserDataRepository userDataRepo, IUserManager userManager)
        {
            const string CreateMediaStreamsTableCommand
                    = "create table if not exists mediastreams (ItemId GUID, StreamIndex INT, StreamType TEXT, Codec TEXT, Language TEXT, ChannelLayout TEXT, Profile TEXT, AspectRatio TEXT, Path TEXT, IsInterlaced BIT, BitRate INT NULL, Channels INT NULL, SampleRate INT NULL, IsDefault BIT, IsForced BIT, IsExternal BIT, Height INT NULL, Width INT NULL, AverageFrameRate FLOAT NULL, RealFrameRate FLOAT NULL, Level FLOAT NULL, PixelFormat TEXT, BitDepth INT NULL, IsAnamorphic BIT NULL, RefFrames INT NULL, CodecTag TEXT NULL, Comment TEXT NULL, NalLengthSize TEXT NULL, IsAvc BIT NULL, Title TEXT NULL, TimeBase TEXT NULL, CodecTimeBase TEXT NULL, ColorPrimaries TEXT NULL, ColorSpace TEXT NULL, ColorTransfer TEXT NULL, DvVersionMajor INT NULL, DvVersionMinor INT NULL, DvProfile INT NULL, DvLevel INT NULL, RpuPresentFlag INT NULL, ElPresentFlag INT NULL, BlPresentFlag INT NULL, DvBlSignalCompatibilityId INT NULL, IsHearingImpaired BIT NULL, PRIMARY KEY (ItemId, StreamIndex))";

            const string CreateMediaAttachmentsTableCommand
                    = "create table if not exists mediaattachments (ItemId GUID, AttachmentIndex INT, Codec TEXT, CodecTag TEXT NULL, Comment TEXT NULL, Filename TEXT NULL, MIMEType TEXT NULL, PRIMARY KEY (ItemId, AttachmentIndex))";

            string[] queries =
            {
                "PRAGMA locking_mode=EXCLUSIVE",

                "create table if not exists TypedBaseItems (guid GUID primary key NOT NULL, type TEXT NOT NULL, data BLOB NULL, ParentId GUID NULL, Path TEXT NULL)",

                "create table if not exists AncestorIds (ItemId GUID NOT NULL, AncestorId GUID NOT NULL, AncestorIdText TEXT NOT NULL, PRIMARY KEY (ItemId, AncestorId))",
                "create index if not exists idx_AncestorIds1 on AncestorIds(AncestorId)",
                "create index if not exists idx_AncestorIds5 on AncestorIds(AncestorIdText,ItemId)",

                "create table if not exists ItemValues (ItemId GUID NOT NULL, Type INT NOT NULL, Value TEXT NOT NULL, CleanValue TEXT NOT NULL)",

                "create table if not exists People (ItemId GUID, Name TEXT NOT NULL, Role TEXT, PersonType TEXT, SortOrder int, ListOrder int)",

                "drop index if exists idxPeopleItemId",
                "create index if not exists idxPeopleItemId1 on People(ItemId,ListOrder)",
                "create index if not exists idxPeopleName on People(Name)",

                "create table if not exists " + ChaptersTableName + " (ItemId GUID, ChapterIndex INT NOT NULL, StartPositionTicks BIGINT NOT NULL, Name TEXT, ImagePath TEXT, PRIMARY KEY (ItemId, ChapterIndex))",

                CreateMediaStreamsTableCommand,
                CreateMediaAttachmentsTableCommand,

                "pragma shrink_memory"
            };

            string[] postQueries =
            {
                // obsolete
                "drop index if exists idx_TypedBaseItems",
                "drop index if exists idx_mediastreams",
                "drop index if exists idx_mediastreams1",
                "drop index if exists idx_" + ChaptersTableName,
                "drop index if exists idx_UserDataKeys1",
                "drop index if exists idx_UserDataKeys2",
                "drop index if exists idx_TypeTopParentId3",
                "drop index if exists idx_TypeTopParentId2",
                "drop index if exists idx_TypeTopParentId4",
                "drop index if exists idx_Type",
                "drop index if exists idx_TypeTopParentId",
                "drop index if exists idx_GuidType",
                "drop index if exists idx_TopParentId",
                "drop index if exists idx_TypeTopParentId6",
                "drop index if exists idx_ItemValues2",
                "drop index if exists Idx_ProviderIds",
                "drop index if exists idx_ItemValues3",
                "drop index if exists idx_ItemValues4",
                "drop index if exists idx_ItemValues5",
                "drop index if exists idx_UserDataKeys3",
                "drop table if exists UserDataKeys",
                "drop table if exists ProviderIds",
                "drop index if exists Idx_ProviderIds1",
                "drop table if exists Images",
                "drop index if exists idx_Images",
                "drop index if exists idx_TypeSeriesPresentationUniqueKey",
                "drop index if exists idx_SeriesPresentationUniqueKey",
                "drop index if exists idx_TypeSeriesPresentationUniqueKey2",
                "drop index if exists idx_AncestorIds3",
                "drop index if exists idx_AncestorIds4",
                "drop index if exists idx_AncestorIds2",

                "create index if not exists idx_PathTypedBaseItems on TypedBaseItems(Path)",
                "create index if not exists idx_ParentIdTypedBaseItems on TypedBaseItems(ParentId)",

                "create index if not exists idx_PresentationUniqueKey on TypedBaseItems(PresentationUniqueKey)",
                "create index if not exists idx_GuidTypeIsFolderIsVirtualItem on TypedBaseItems(Guid,Type,IsFolder,IsVirtualItem)",
                "create index if not exists idx_CleanNameType on TypedBaseItems(CleanName,Type)",

                // covering index
                "create index if not exists idx_TopParentIdGuid on TypedBaseItems(TopParentId,Guid)",

                // series
                "create index if not exists idx_TypeSeriesPresentationUniqueKey1 on TypedBaseItems(Type,SeriesPresentationUniqueKey,PresentationUniqueKey,SortName)",

                // series counts
                // seriesdateplayed sort order
                "create index if not exists idx_TypeSeriesPresentationUniqueKey3 on TypedBaseItems(SeriesPresentationUniqueKey,Type,IsFolder,IsVirtualItem)",

                // live tv programs
                "create index if not exists idx_TypeTopParentIdStartDate on TypedBaseItems(Type,TopParentId,StartDate)",

                // covering index for getitemvalues
                "create index if not exists idx_TypeTopParentIdGuid on TypedBaseItems(Type,TopParentId,Guid)",

                // used by movie suggestions
                "create index if not exists idx_TypeTopParentIdGroup on TypedBaseItems(Type,TopParentId,PresentationUniqueKey)",
                "create index if not exists idx_TypeTopParentId5 on TypedBaseItems(TopParentId,IsVirtualItem)",

                // latest items
                "create index if not exists idx_TypeTopParentId9 on TypedBaseItems(TopParentId,Type,IsVirtualItem,PresentationUniqueKey,DateCreated)",
                "create index if not exists idx_TypeTopParentId8 on TypedBaseItems(TopParentId,IsFolder,IsVirtualItem,PresentationUniqueKey,DateCreated)",

                // resume
                "create index if not exists idx_TypeTopParentId7 on TypedBaseItems(TopParentId,MediaType,IsVirtualItem,PresentationUniqueKey)",

                // items by name
                "create index if not exists idx_ItemValues6 on ItemValues(ItemId,Type,CleanValue)",
                "create index if not exists idx_ItemValues7 on ItemValues(Type,CleanValue,ItemId)",

                // Used to update inherited tags
                "create index if not exists idx_ItemValues8 on ItemValues(Type, ItemId, Value)",
            };

            using (var connection = GetConnection())
            {
                connection.RunQueries(queries);

                connection.RunInTransaction(
                    db =>
                    {
                        var existingColumnNames = GetColumnNames(db, "AncestorIds");
                        AddColumn(db, "AncestorIds", "AncestorIdText", "Text", existingColumnNames);

                        existingColumnNames = GetColumnNames(db, "TypedBaseItems");

                        AddColumn(db, "TypedBaseItems", "Path", "Text", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "StartDate", "DATETIME", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "EndDate", "DATETIME", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "ChannelId", "Text", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "IsMovie", "BIT", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "CommunityRating", "Float", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "CustomRating", "Text", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "IndexNumber", "INT", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "IsLocked", "BIT", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "Name", "Text", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "OfficialRating", "Text", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "MediaType", "Text", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "Overview", "Text", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "ParentIndexNumber", "INT", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "PremiereDate", "DATETIME", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "ProductionYear", "INT", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "ParentId", "GUID", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "Genres", "Text", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "SortName", "Text", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "ForcedSortName", "Text", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "RunTimeTicks", "BIGINT", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "DateCreated", "DATETIME", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "DateModified", "DATETIME", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "IsSeries", "BIT", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "EpisodeTitle", "Text", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "IsRepeat", "BIT", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "PreferredMetadataLanguage", "Text", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "PreferredMetadataCountryCode", "Text", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "DateLastRefreshed", "DATETIME", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "DateLastSaved", "DATETIME", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "IsInMixedFolder", "BIT", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "LockedFields", "Text", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "Studios", "Text", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "Audio", "Text", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "ExternalServiceId", "Text", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "Tags", "Text", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "IsFolder", "BIT", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "InheritedParentalRatingValue", "INT", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "UnratedType", "Text", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "TopParentId", "Text", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "TrailerTypes", "Text", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "CriticRating", "Float", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "CleanName", "Text", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "PresentationUniqueKey", "Text", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "OriginalTitle", "Text", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "PrimaryVersionId", "Text", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "DateLastMediaAdded", "DATETIME", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "Album", "Text", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "IsVirtualItem", "BIT", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "SeriesName", "Text", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "UserDataKey", "Text", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "SeasonName", "Text", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "SeasonId", "GUID", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "SeriesId", "GUID", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "ExternalSeriesId", "Text", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "Tagline", "Text", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "ProviderIds", "Text", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "Images", "Text", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "ProductionLocations", "Text", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "ExtraIds", "Text", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "TotalBitrate", "INT", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "ExtraType", "Text", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "Artists", "Text", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "AlbumArtists", "Text", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "ExternalId", "Text", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "SeriesPresentationUniqueKey", "Text", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "ShowId", "Text", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "OwnerId", "Text", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "Width", "INT", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "Height", "INT", existingColumnNames);
                        AddColumn(db, "TypedBaseItems", "Size", "BIGINT", existingColumnNames);

                        existingColumnNames = GetColumnNames(db, "ItemValues");
                        AddColumn(db, "ItemValues", "CleanValue", "Text", existingColumnNames);

                        existingColumnNames = GetColumnNames(db, ChaptersTableName);
                        AddColumn(db, ChaptersTableName, "ImageDateModified", "DATETIME", existingColumnNames);

                        existingColumnNames = GetColumnNames(db, "MediaStreams");
                        AddColumn(db, "MediaStreams", "IsAvc", "BIT", existingColumnNames);
                        AddColumn(db, "MediaStreams", "TimeBase", "TEXT", existingColumnNames);
                        AddColumn(db, "MediaStreams", "CodecTimeBase", "TEXT", existingColumnNames);
                        AddColumn(db, "MediaStreams", "Title", "TEXT", existingColumnNames);
                        AddColumn(db, "MediaStreams", "NalLengthSize", "TEXT", existingColumnNames);
                        AddColumn(db, "MediaStreams", "Comment", "TEXT", existingColumnNames);
                        AddColumn(db, "MediaStreams", "CodecTag", "TEXT", existingColumnNames);
                        AddColumn(db, "MediaStreams", "PixelFormat", "TEXT", existingColumnNames);
                        AddColumn(db, "MediaStreams", "BitDepth", "INT", existingColumnNames);
                        AddColumn(db, "MediaStreams", "RefFrames", "INT", existingColumnNames);
                        AddColumn(db, "MediaStreams", "KeyFrames", "TEXT", existingColumnNames);
                        AddColumn(db, "MediaStreams", "IsAnamorphic", "BIT", existingColumnNames);

                        AddColumn(db, "MediaStreams", "ColorPrimaries", "TEXT", existingColumnNames);
                        AddColumn(db, "MediaStreams", "ColorSpace", "TEXT", existingColumnNames);
                        AddColumn(db, "MediaStreams", "ColorTransfer", "TEXT", existingColumnNames);

                        AddColumn(db, "MediaStreams", "DvVersionMajor", "INT", existingColumnNames);
                        AddColumn(db, "MediaStreams", "DvVersionMinor", "INT", existingColumnNames);
                        AddColumn(db, "MediaStreams", "DvProfile", "INT", existingColumnNames);
                        AddColumn(db, "MediaStreams", "DvLevel", "INT", existingColumnNames);
                        AddColumn(db, "MediaStreams", "RpuPresentFlag", "INT", existingColumnNames);
                        AddColumn(db, "MediaStreams", "ElPresentFlag", "INT", existingColumnNames);
                        AddColumn(db, "MediaStreams", "BlPresentFlag", "INT", existingColumnNames);
                        AddColumn(db, "MediaStreams", "DvBlSignalCompatibilityId", "INT", existingColumnNames);

                        AddColumn(db, "MediaStreams", "IsHearingImpaired", "BIT", existingColumnNames);
                    },
                    TransactionMode);

                connection.RunQueries(postQueries);
            }

            userDataRepo.Initialize(userManager, WriteLock, WriteConnection);
        }

        public void SaveImages(BaseItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            CheckDisposed();

            using (var connection = GetConnection())
            {
                connection.RunInTransaction(
                    db =>
                    {
                        using (var saveImagesStatement = PrepareStatement(db, "Update TypedBaseItems set Images=@Images where guid=@Id"))
                        {
                            saveImagesStatement.TryBind("@Id", item.Id);
                            saveImagesStatement.TryBind("@Images", SerializeImages(item.ImageInfos));

                            saveImagesStatement.MoveNext();
                        }
                    },
                    TransactionMode);
            }
        }

        /// <summary>
        /// Saves the items.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="items"/> or <paramref name="cancellationToken"/> is <c>null</c>.
        /// </exception>
        public void SaveItems(IEnumerable<BaseItem> items, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(items);

            cancellationToken.ThrowIfCancellationRequested();

            CheckDisposed();

            var tuples = new List<(BaseItem, List<Guid>, BaseItem, string, List<string>)>();
            foreach (var item in items)
            {
                var ancestorIds = item.SupportsAncestors ?
                    item.GetAncestorIds().Distinct().ToList() :
                    null;

                var topParent = item.GetTopParent();

                var userdataKey = item.GetUserDataKeys().FirstOrDefault();
                var inheritedTags = item.GetInheritedTags();

                tuples.Add((item, ancestorIds, topParent, userdataKey, inheritedTags));
            }

            using (var connection = GetConnection())
            {
                connection.RunInTransaction(
                    db =>
                    {
                        SaveItemsInTransaction(db, tuples);
                    },
                    TransactionMode);
            }
        }

        private void SaveItemsInTransaction(IDatabaseConnection db, IEnumerable<(BaseItem Item, List<Guid> AncestorIds, BaseItem TopParent, string UserDataKey, List<string> InheritedTags)> tuples)
        {
            var statements = PrepareAll(db, new string[]
            {
                SaveItemCommandText,
                "delete from AncestorIds where ItemId=@ItemId"
            });

            using (var saveItemStatement = statements[0])
            using (var deleteAncestorsStatement = statements[1])
            {
                var requiresReset = false;
                foreach (var tuple in tuples)
                {
                    if (requiresReset)
                    {
                        saveItemStatement.Reset();
                    }

                    var item = tuple.Item;
                    var topParent = tuple.TopParent;
                    var userDataKey = tuple.UserDataKey;

                    SaveItem(item, topParent, userDataKey, saveItemStatement);

                    var inheritedTags = tuple.InheritedTags;

                    if (item.SupportsAncestors)
                    {
                        UpdateAncestors(item.Id, tuple.AncestorIds, db, deleteAncestorsStatement);
                    }

                    UpdateItemValues(item.Id, GetItemValuesToSave(item, inheritedTags), db);

                    requiresReset = true;
                }
            }
        }

        private string GetPathToSave(string path)
        {
            if (path == null)
            {
                return null;
            }

            return _appHost.ReverseVirtualPath(path);
        }

        private string RestorePath(string path)
        {
            return _appHost.ExpandVirtualPath(path);
        }

        private void SaveItem(BaseItem item, BaseItem topParent, string userDataKey, IStatement saveItemStatement)
        {
            Type type = item.GetType();

            saveItemStatement.TryBind("@guid", item.Id);
            saveItemStatement.TryBind("@type", type.FullName);

            if (TypeRequiresDeserialization(type))
            {
                saveItemStatement.TryBind("@data", JsonSerializer.SerializeToUtf8Bytes(item, type, _jsonOptions));
            }
            else
            {
                saveItemStatement.TryBindNull("@data");
            }

            saveItemStatement.TryBind("@Path", GetPathToSave(item.Path));

            if (item is IHasStartDate hasStartDate)
            {
                saveItemStatement.TryBind("@StartDate", hasStartDate.StartDate);
            }
            else
            {
                saveItemStatement.TryBindNull("@StartDate");
            }

            if (item.EndDate.HasValue)
            {
                saveItemStatement.TryBind("@EndDate", item.EndDate.Value);
            }
            else
            {
                saveItemStatement.TryBindNull("@EndDate");
            }

            saveItemStatement.TryBind("@ChannelId", item.ChannelId.Equals(default) ? null : item.ChannelId.ToString("N", CultureInfo.InvariantCulture));

            if (item is IHasProgramAttributes hasProgramAttributes)
            {
                saveItemStatement.TryBind("@IsMovie", hasProgramAttributes.IsMovie);
                saveItemStatement.TryBind("@IsSeries", hasProgramAttributes.IsSeries);
                saveItemStatement.TryBind("@EpisodeTitle", hasProgramAttributes.EpisodeTitle);
                saveItemStatement.TryBind("@IsRepeat", hasProgramAttributes.IsRepeat);
            }
            else
            {
                saveItemStatement.TryBindNull("@IsMovie");
                saveItemStatement.TryBindNull("@IsSeries");
                saveItemStatement.TryBindNull("@EpisodeTitle");
                saveItemStatement.TryBindNull("@IsRepeat");
            }

            saveItemStatement.TryBind("@CommunityRating", item.CommunityRating);
            saveItemStatement.TryBind("@CustomRating", item.CustomRating);
            saveItemStatement.TryBind("@IndexNumber", item.IndexNumber);
            saveItemStatement.TryBind("@IsLocked", item.IsLocked);
            saveItemStatement.TryBind("@Name", item.Name);
            saveItemStatement.TryBind("@OfficialRating", item.OfficialRating);
            saveItemStatement.TryBind("@MediaType", item.MediaType);
            saveItemStatement.TryBind("@Overview", item.Overview);
            saveItemStatement.TryBind("@ParentIndexNumber", item.ParentIndexNumber);
            saveItemStatement.TryBind("@PremiereDate", item.PremiereDate);
            saveItemStatement.TryBind("@ProductionYear", item.ProductionYear);

            var parentId = item.ParentId;
            if (parentId.Equals(default))
            {
                saveItemStatement.TryBindNull("@ParentId");
            }
            else
            {
                saveItemStatement.TryBind("@ParentId", parentId);
            }

            if (item.Genres.Length > 0)
            {
                saveItemStatement.TryBind("@Genres", string.Join('|', item.Genres));
            }
            else
            {
                saveItemStatement.TryBindNull("@Genres");
            }

            saveItemStatement.TryBind("@InheritedParentalRatingValue", item.InheritedParentalRatingValue);

            saveItemStatement.TryBind("@SortName", item.SortName);

            saveItemStatement.TryBind("@ForcedSortName", item.ForcedSortName);

            saveItemStatement.TryBind("@RunTimeTicks", item.RunTimeTicks);
            saveItemStatement.TryBind("@Size", item.Size);

            saveItemStatement.TryBind("@DateCreated", item.DateCreated);
            saveItemStatement.TryBind("@DateModified", item.DateModified);

            saveItemStatement.TryBind("@PreferredMetadataLanguage", item.PreferredMetadataLanguage);
            saveItemStatement.TryBind("@PreferredMetadataCountryCode", item.PreferredMetadataCountryCode);

            if (item.Width > 0)
            {
                saveItemStatement.TryBind("@Width", item.Width);
            }
            else
            {
                saveItemStatement.TryBindNull("@Width");
            }

            if (item.Height > 0)
            {
                saveItemStatement.TryBind("@Height", item.Height);
            }
            else
            {
                saveItemStatement.TryBindNull("@Height");
            }

            if (item.DateLastRefreshed != default(DateTime))
            {
                saveItemStatement.TryBind("@DateLastRefreshed", item.DateLastRefreshed);
            }
            else
            {
                saveItemStatement.TryBindNull("@DateLastRefreshed");
            }

            if (item.DateLastSaved != default(DateTime))
            {
                saveItemStatement.TryBind("@DateLastSaved", item.DateLastSaved);
            }
            else
            {
                saveItemStatement.TryBindNull("@DateLastSaved");
            }

            saveItemStatement.TryBind("@IsInMixedFolder", item.IsInMixedFolder);

            if (item.LockedFields.Length > 0)
            {
                saveItemStatement.TryBind("@LockedFields", string.Join('|', item.LockedFields));
            }
            else
            {
                saveItemStatement.TryBindNull("@LockedFields");
            }

            if (item.Studios.Length > 0)
            {
                saveItemStatement.TryBind("@Studios", string.Join('|', item.Studios));
            }
            else
            {
                saveItemStatement.TryBindNull("@Studios");
            }

            if (item.Audio.HasValue)
            {
                saveItemStatement.TryBind("@Audio", item.Audio.Value.ToString());
            }
            else
            {
                saveItemStatement.TryBindNull("@Audio");
            }

            if (item is LiveTvChannel liveTvChannel)
            {
                saveItemStatement.TryBind("@ExternalServiceId", liveTvChannel.ServiceName);
            }
            else
            {
                saveItemStatement.TryBindNull("@ExternalServiceId");
            }

            if (item.Tags.Length > 0)
            {
                saveItemStatement.TryBind("@Tags", string.Join('|', item.Tags));
            }
            else
            {
                saveItemStatement.TryBindNull("@Tags");
            }

            saveItemStatement.TryBind("@IsFolder", item.IsFolder);

            saveItemStatement.TryBind("@UnratedType", item.GetBlockUnratedType().ToString());

            if (topParent == null)
            {
                saveItemStatement.TryBindNull("@TopParentId");
            }
            else
            {
                saveItemStatement.TryBind("@TopParentId", topParent.Id.ToString("N", CultureInfo.InvariantCulture));
            }

            if (item is Trailer trailer && trailer.TrailerTypes.Length > 0)
            {
                saveItemStatement.TryBind("@TrailerTypes", string.Join('|', trailer.TrailerTypes));
            }
            else
            {
                saveItemStatement.TryBindNull("@TrailerTypes");
            }

            saveItemStatement.TryBind("@CriticRating", item.CriticRating);

            if (string.IsNullOrWhiteSpace(item.Name))
            {
                saveItemStatement.TryBindNull("@CleanName");
            }
            else
            {
                saveItemStatement.TryBind("@CleanName", GetCleanValue(item.Name));
            }

            saveItemStatement.TryBind("@PresentationUniqueKey", item.PresentationUniqueKey);
            saveItemStatement.TryBind("@OriginalTitle", item.OriginalTitle);

            if (item is Video video)
            {
                saveItemStatement.TryBind("@PrimaryVersionId", video.PrimaryVersionId);
            }
            else
            {
                saveItemStatement.TryBindNull("@PrimaryVersionId");
            }

            if (item is Folder folder && folder.DateLastMediaAdded.HasValue)
            {
                saveItemStatement.TryBind("@DateLastMediaAdded", folder.DateLastMediaAdded.Value);
            }
            else
            {
                saveItemStatement.TryBindNull("@DateLastMediaAdded");
            }

            saveItemStatement.TryBind("@Album", item.Album);
            saveItemStatement.TryBind("@IsVirtualItem", item.IsVirtualItem);

            if (item is IHasSeries hasSeriesName)
            {
                saveItemStatement.TryBind("@SeriesName", hasSeriesName.SeriesName);
            }
            else
            {
                saveItemStatement.TryBindNull("@SeriesName");
            }

            if (string.IsNullOrWhiteSpace(userDataKey))
            {
                saveItemStatement.TryBindNull("@UserDataKey");
            }
            else
            {
                saveItemStatement.TryBind("@UserDataKey", userDataKey);
            }

            if (item is Episode episode)
            {
                saveItemStatement.TryBind("@SeasonName", episode.SeasonName);

                var nullableSeasonId = episode.SeasonId.Equals(default) ? (Guid?)null : episode.SeasonId;

                saveItemStatement.TryBind("@SeasonId", nullableSeasonId);
            }
            else
            {
                saveItemStatement.TryBindNull("@SeasonName");
                saveItemStatement.TryBindNull("@SeasonId");
            }

            if (item is IHasSeries hasSeries)
            {
                var nullableSeriesId = hasSeries.SeriesId.Equals(default) ? (Guid?)null : hasSeries.SeriesId;

                saveItemStatement.TryBind("@SeriesId", nullableSeriesId);
                saveItemStatement.TryBind("@SeriesPresentationUniqueKey", hasSeries.SeriesPresentationUniqueKey);
            }
            else
            {
                saveItemStatement.TryBindNull("@SeriesId");
                saveItemStatement.TryBindNull("@SeriesPresentationUniqueKey");
            }

            saveItemStatement.TryBind("@ExternalSeriesId", item.ExternalSeriesId);
            saveItemStatement.TryBind("@Tagline", item.Tagline);

            saveItemStatement.TryBind("@ProviderIds", SerializeProviderIds(item.ProviderIds));
            saveItemStatement.TryBind("@Images", SerializeImages(item.ImageInfos));

            if (item.ProductionLocations.Length > 0)
            {
                saveItemStatement.TryBind("@ProductionLocations", string.Join('|', item.ProductionLocations));
            }
            else
            {
                saveItemStatement.TryBindNull("@ProductionLocations");
            }

            if (item.ExtraIds.Length > 0)
            {
                saveItemStatement.TryBind("@ExtraIds", string.Join('|', item.ExtraIds));
            }
            else
            {
                saveItemStatement.TryBindNull("@ExtraIds");
            }

            saveItemStatement.TryBind("@TotalBitrate", item.TotalBitrate);
            if (item.ExtraType.HasValue)
            {
                saveItemStatement.TryBind("@ExtraType", item.ExtraType.Value.ToString());
            }
            else
            {
                saveItemStatement.TryBindNull("@ExtraType");
            }

            string artists = null;
            if (item is IHasArtist hasArtists && hasArtists.Artists.Count > 0)
            {
                artists = string.Join('|', hasArtists.Artists);
            }

            saveItemStatement.TryBind("@Artists", artists);

            string albumArtists = null;
            if (item is IHasAlbumArtist hasAlbumArtists
                && hasAlbumArtists.AlbumArtists.Count > 0)
            {
                albumArtists = string.Join('|', hasAlbumArtists.AlbumArtists);
            }

            saveItemStatement.TryBind("@AlbumArtists", albumArtists);
            saveItemStatement.TryBind("@ExternalId", item.ExternalId);

            if (item is LiveTvProgram program)
            {
                saveItemStatement.TryBind("@ShowId", program.ShowId);
            }
            else
            {
                saveItemStatement.TryBindNull("@ShowId");
            }

            Guid ownerId = item.OwnerId;
            if (ownerId.Equals(default))
            {
                saveItemStatement.TryBindNull("@OwnerId");
            }
            else
            {
                saveItemStatement.TryBind("@OwnerId", ownerId);
            }

            saveItemStatement.MoveNext();
        }

        internal static string SerializeProviderIds(Dictionary<string, string> providerIds)
        {
            StringBuilder str = new StringBuilder();
            foreach (var i in providerIds)
            {
                // Ideally we shouldn't need this IsNullOrWhiteSpace check,
                // but we're seeing some cases of bad data slip through
                if (string.IsNullOrWhiteSpace(i.Value))
                {
                    continue;
                }

                str.Append(i.Key)
                    .Append('=')
                    .Append(i.Value)
                    .Append('|');
            }

            if (str.Length == 0)
            {
                return null;
            }

            str.Length -= 1; // Remove last |
            return str.ToString();
        }

        internal static void DeserializeProviderIds(string value, IHasProviderIds item)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            foreach (var part in value.SpanSplit('|'))
            {
                var providerDelimiterIndex = part.IndexOf('=');
                if (providerDelimiterIndex != -1 && providerDelimiterIndex == part.LastIndexOf('='))
                {
                    item.SetProviderId(part.Slice(0, providerDelimiterIndex).ToString(), part.Slice(providerDelimiterIndex + 1).ToString());
                }
            }
        }

        internal string SerializeImages(ItemImageInfo[] images)
        {
            if (images.Length == 0)
            {
                return null;
            }

            StringBuilder str = new StringBuilder();
            foreach (var i in images)
            {
                if (string.IsNullOrWhiteSpace(i.Path))
                {
                    continue;
                }

                AppendItemImageInfo(str, i);
                str.Append('|');
            }

            str.Length -= 1; // Remove last |
            return str.ToString();
        }

        internal ItemImageInfo[] DeserializeImages(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Array.Empty<ItemImageInfo>();
            }

            // TODO The following is an ugly performance optimization, but it's extremely unlikely that the data in the database would be malformed
            var valueSpan = value.AsSpan();
            var count = valueSpan.Count('|') + 1;

            var position = 0;
            var result = new ItemImageInfo[count];
            foreach (var part in valueSpan.Split('|'))
            {
                var image = ItemImageInfoFromValueString(part);

                if (image != null)
                {
                    result[position++] = image;
                }
            }

            if (position == count)
            {
                return result;
            }

            if (position == 0)
            {
                return Array.Empty<ItemImageInfo>();
            }

            // Extremely unlikely, but somehow one or more of the image strings were malformed. Cut the array.
            return result[..position];
        }

        private void AppendItemImageInfo(StringBuilder bldr, ItemImageInfo image)
        {
            const char Delimiter = '*';

            var path = image.Path ?? string.Empty;

            bldr.Append(GetPathToSave(path))
                .Append(Delimiter)
                .Append(image.DateModified.Ticks)
                .Append(Delimiter)
                .Append(image.Type)
                .Append(Delimiter)
                .Append(image.Width)
                .Append(Delimiter)
                .Append(image.Height);

            var hash = image.BlurHash;
            if (!string.IsNullOrEmpty(hash))
            {
                bldr.Append(Delimiter)
                    // Replace delimiters with other characters.
                    // This can be removed when we migrate to a proper DB.
                    .Append(hash.Replace(Delimiter, '/').Replace('|', '\\'));
            }
        }

        internal ItemImageInfo ItemImageInfoFromValueString(ReadOnlySpan<char> value)
        {
            const char Delimiter = '*';

            var nextSegment = value.IndexOf(Delimiter);
            if (nextSegment == -1)
            {
                return null;
            }

            ReadOnlySpan<char> path = value[..nextSegment];
            value = value[(nextSegment + 1)..];
            nextSegment = value.IndexOf(Delimiter);
            if (nextSegment == -1)
            {
                return null;
            }

            ReadOnlySpan<char> dateModified = value[..nextSegment];
            value = value[(nextSegment + 1)..];
            nextSegment = value.IndexOf(Delimiter);
            if (nextSegment == -1)
            {
                nextSegment = value.Length;
            }

            ReadOnlySpan<char> imageType = value[..nextSegment];

            var image = new ItemImageInfo
            {
                Path = RestorePath(path.ToString())
            };

            if (long.TryParse(dateModified, NumberStyles.Any, CultureInfo.InvariantCulture, out var ticks)
                && ticks >= DateTime.MinValue.Ticks
                && ticks <= DateTime.MaxValue.Ticks)
            {
                image.DateModified = new DateTime(ticks, DateTimeKind.Utc);
            }
            else
            {
                return null;
            }

            if (Enum.TryParse(imageType, true, out ImageType type))
            {
                image.Type = type;
            }
            else
            {
                return null;
            }

            // Optional parameters: width*height*blurhash
            if (nextSegment + 1 < value.Length - 1)
            {
                value = value[(nextSegment + 1)..];
                nextSegment = value.IndexOf(Delimiter);
                if (nextSegment == -1 || nextSegment == value.Length)
                {
                    return image;
                }

                ReadOnlySpan<char> widthSpan = value[..nextSegment];

                value = value[(nextSegment + 1)..];
                nextSegment = value.IndexOf(Delimiter);
                if (nextSegment == -1)
                {
                    nextSegment = value.Length;
                }

                ReadOnlySpan<char> heightSpan = value[..nextSegment];

                if (int.TryParse(widthSpan, NumberStyles.Integer, CultureInfo.InvariantCulture, out var width)
                    && int.TryParse(heightSpan, NumberStyles.Integer, CultureInfo.InvariantCulture, out var height))
                {
                    image.Width = width;
                    image.Height = height;
                }

                if (nextSegment < value.Length - 1)
                {
                    value = value[(nextSegment + 1)..];
                    var length = value.Length;

                    Span<char> blurHashSpan = stackalloc char[length];
                    for (int i = 0; i < length; i++)
                    {
                        var c = value[i];
                        blurHashSpan[i] = c switch
                        {
                            '/' => Delimiter,
                            '\\' => '|',
                            _ => c
                        };
                    }

                    image.BlurHash = new string(blurHashSpan);
                }
            }

            return image;
        }

        /// <summary>
        /// Internal retrieve from items or users table.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>BaseItem.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="id"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramr name="id"/> is <seealso cref="Guid.Empty"/>.</exception>
        public BaseItem RetrieveItem(Guid id)
        {
            if (id.Equals(default))
            {
                throw new ArgumentException("Guid can't be empty", nameof(id));
            }

            CheckDisposed();

            using (var connection = GetConnection(true))
            {
                using (var statement = PrepareStatement(connection, _retrieveItemColumnsSelectQuery))
                {
                    statement.TryBind("@guid", id);

                    foreach (var row in statement.ExecuteQuery())
                    {
                        return GetItem(row, new InternalItemsQuery());
                    }
                }
            }

            return null;
        }

        private bool TypeRequiresDeserialization(Type type)
        {
            if (_config.Configuration.SkipDeserializationForBasicTypes)
            {
                if (type == typeof(Channel))
                {
                    return false;
                }
                else if (type == typeof(UserRootFolder))
                {
                    return false;
                }
            }

            if (type == typeof(Season))
            {
                return false;
            }
            else if (type == typeof(MusicArtist))
            {
                return false;
            }
            else if (type == typeof(Person))
            {
                return false;
            }
            else if (type == typeof(MusicGenre))
            {
                return false;
            }
            else if (type == typeof(Genre))
            {
                return false;
            }
            else if (type == typeof(Studio))
            {
                return false;
            }
            else if (type == typeof(PlaylistsFolder))
            {
                return false;
            }
            else if (type == typeof(PhotoAlbum))
            {
                return false;
            }
            else if (type == typeof(Year))
            {
                return false;
            }
            else if (type == typeof(Book))
            {
                return false;
            }
            else if (type == typeof(LiveTvProgram))
            {
                return false;
            }
            else if (type == typeof(AudioBook))
            {
                return false;
            }
            else if (type == typeof(Audio))
            {
                return false;
            }
            else if (type == typeof(MusicAlbum))
            {
                return false;
            }

            return true;
        }

        private BaseItem GetItem(IReadOnlyList<ResultSetValue> reader, InternalItemsQuery query)
        {
            return GetItem(reader, query, HasProgramAttributes(query), HasEpisodeAttributes(query), HasServiceName(query), HasStartDate(query), HasTrailerTypes(query), HasArtistFields(query), HasSeriesFields(query));
        }

        private BaseItem GetItem(IReadOnlyList<ResultSetValue> reader, InternalItemsQuery query, bool enableProgramAttributes, bool hasEpisodeAttributes, bool hasServiceName, bool queryHasStartDate, bool hasTrailerTypes, bool hasArtistFields, bool hasSeriesFields)
        {
            var typeString = reader.GetString(0);

            var type = _typeMapper.GetType(typeString);

            if (type == null)
            {
                return null;
            }

            BaseItem item = null;

            if (TypeRequiresDeserialization(type))
            {
                try
                {
                    item = JsonSerializer.Deserialize(reader[1].ToBlob(), type, _jsonOptions) as BaseItem;
                }
                catch (JsonException ex)
                {
                    Logger.LogError(ex, "Error deserializing item with JSON: {Data}", reader.GetString(1));
                }
            }

            if (item == null)
            {
                try
                {
                    item = Activator.CreateInstance(type) as BaseItem;
                }
                catch
                {
                }
            }

            if (item == null)
            {
                return null;
            }

            var index = 2;

            if (queryHasStartDate)
            {
                if (item is IHasStartDate hasStartDate && reader.TryReadDateTime(index, out var startDate))
                {
                    hasStartDate.StartDate = startDate;
                }

                index++;
            }

            if (reader.TryReadDateTime(index++, out var endDate))
            {
                item.EndDate = endDate;
            }

            var channelId = reader[index];
            if (!channelId.IsDbNull())
            {
                if (!Utf8Parser.TryParse(channelId.ToBlob(), out Guid value, out _, standardFormat: 'N'))
                {
                    var str = reader.GetString(index);
                    Logger.LogWarning("{ChannelId} isn't in the expected format", str);
                    value = new Guid(str);
                }

                item.ChannelId = value;
            }

            index++;

            if (enableProgramAttributes)
            {
                if (item is IHasProgramAttributes hasProgramAttributes)
                {
                    if (reader.TryGetBoolean(index++, out var isMovie))
                    {
                        hasProgramAttributes.IsMovie = isMovie;
                    }

                    if (reader.TryGetBoolean(index++, out var isSeries))
                    {
                        hasProgramAttributes.IsSeries = isSeries;
                    }

                    if (reader.TryGetString(index++, out var episodeTitle))
                    {
                        hasProgramAttributes.EpisodeTitle = episodeTitle;
                    }

                    if (reader.TryGetBoolean(index++, out var isRepeat))
                    {
                        hasProgramAttributes.IsRepeat = isRepeat;
                    }
                }
                else
                {
                    index += 4;
                }
            }

            if (reader.TryGetSingle(index++, out var communityRating))
            {
                item.CommunityRating = communityRating;
            }

            if (HasField(query, ItemFields.CustomRating))
            {
                if (reader.TryGetString(index++, out var customRating))
                {
                    item.CustomRating = customRating;
                }
            }

            if (reader.TryGetInt32(index++, out var indexNumber))
            {
                item.IndexNumber = indexNumber;
            }

            if (HasField(query, ItemFields.Settings))
            {
                if (reader.TryGetBoolean(index++, out var isLocked))
                {
                    item.IsLocked = isLocked;
                }

                if (reader.TryGetString(index++, out var preferredMetadataLanguage))
                {
                    item.PreferredMetadataLanguage = preferredMetadataLanguage;
                }

                if (reader.TryGetString(index++, out var preferredMetadataCountryCode))
                {
                    item.PreferredMetadataCountryCode = preferredMetadataCountryCode;
                }
            }

            if (HasField(query, ItemFields.Width))
            {
                if (reader.TryGetInt32(index++, out var width))
                {
                    item.Width = width;
                }
            }

            if (HasField(query, ItemFields.Height))
            {
                if (reader.TryGetInt32(index++, out var height))
                {
                    item.Height = height;
                }
            }

            if (HasField(query, ItemFields.DateLastRefreshed))
            {
                if (reader.TryReadDateTime(index++, out var dateLastRefreshed))
                {
                    item.DateLastRefreshed = dateLastRefreshed;
                }
            }

            if (reader.TryGetString(index++, out var name))
            {
                item.Name = name;
            }

            if (reader.TryGetString(index++, out var restorePath))
            {
                item.Path = RestorePath(restorePath);
            }

            if (reader.TryReadDateTime(index++, out var premiereDate))
            {
                item.PremiereDate = premiereDate;
            }

            if (HasField(query, ItemFields.Overview))
            {
                if (reader.TryGetString(index++, out var overview))
                {
                    item.Overview = overview;
                }
            }

            if (reader.TryGetInt32(index++, out var parentIndexNumber))
            {
                item.ParentIndexNumber = parentIndexNumber;
            }

            if (reader.TryGetInt32(index++, out var productionYear))
            {
                item.ProductionYear = productionYear;
            }

            if (reader.TryGetString(index++, out var officialRating))
            {
                item.OfficialRating = officialRating;
            }

            if (HasField(query, ItemFields.SortName))
            {
                if (reader.TryGetString(index++, out var forcedSortName))
                {
                    item.ForcedSortName = forcedSortName;
                }
            }

            if (reader.TryGetInt64(index++, out var runTimeTicks))
            {
                item.RunTimeTicks = runTimeTicks;
            }

            if (reader.TryGetInt64(index++, out var size))
            {
                item.Size = size;
            }

            if (HasField(query, ItemFields.DateCreated))
            {
                if (reader.TryReadDateTime(index++, out var dateCreated))
                {
                    item.DateCreated = dateCreated;
                }
            }

            if (reader.TryReadDateTime(index++, out var dateModified))
            {
                item.DateModified = dateModified;
            }

            item.Id = reader.GetGuid(index++);

            if (HasField(query, ItemFields.Genres))
            {
                if (reader.TryGetString(index++, out var genres))
                {
                    item.Genres = genres.Split('|', StringSplitOptions.RemoveEmptyEntries);
                }
            }

            if (reader.TryGetGuid(index++, out var parentId))
            {
                item.ParentId = parentId;
            }

            if (reader.TryGetString(index++, out var audioString))
            {
                if (Enum.TryParse(audioString, true, out ProgramAudio audio))
                {
                    item.Audio = audio;
                }
            }

            // TODO: Even if not needed by apps, the server needs it internally
            // But get this excluded from contexts where it is not needed
            if (hasServiceName)
            {
                if (item is LiveTvChannel liveTvChannel)
                {
                    if (reader.TryGetString(index, out var serviceName))
                    {
                        liveTvChannel.ServiceName = serviceName;
                    }
                }

                index++;
            }

            if (reader.TryGetBoolean(index++, out var isInMixedFolder))
            {
                item.IsInMixedFolder = isInMixedFolder;
            }

            if (HasField(query, ItemFields.DateLastSaved))
            {
                if (reader.TryReadDateTime(index++, out var dateLastSaved))
                {
                    item.DateLastSaved = dateLastSaved;
                }
            }

            if (HasField(query, ItemFields.Settings))
            {
                if (reader.TryGetString(index++, out var lockedFields))
                {
                    List<MetadataField> fields = null;
                    foreach (var i in lockedFields.AsSpan().Split('|'))
                    {
                        if (Enum.TryParse(i, true, out MetadataField parsedValue))
                        {
                            (fields ??= new List<MetadataField>()).Add(parsedValue);
                        }
                    }

                    item.LockedFields = fields?.ToArray() ?? Array.Empty<MetadataField>();
                }
            }

            if (HasField(query, ItemFields.Studios))
            {
                if (reader.TryGetString(index++, out var studios))
                {
                    item.Studios = studios.Split('|', StringSplitOptions.RemoveEmptyEntries);
                }
            }

            if (HasField(query, ItemFields.Tags))
            {
                if (reader.TryGetString(index++, out var tags))
                {
                    item.Tags = tags.Split('|', StringSplitOptions.RemoveEmptyEntries);
                }
            }

            if (hasTrailerTypes)
            {
                if (item is Trailer trailer)
                {
                    if (reader.TryGetString(index, out var trailerTypes))
                    {
                        List<TrailerType> types = null;
                        foreach (var i in trailerTypes.AsSpan().Split('|'))
                        {
                            if (Enum.TryParse(i, true, out TrailerType parsedValue))
                            {
                                (types ??= new List<TrailerType>()).Add(parsedValue);
                            }
                        }

                        trailer.TrailerTypes = types?.ToArray() ?? Array.Empty<TrailerType>();
                    }
                }

                index++;
            }

            if (HasField(query, ItemFields.OriginalTitle))
            {
                if (reader.TryGetString(index++, out var originalTitle))
                {
                    item.OriginalTitle = originalTitle;
                }
            }

            if (item is Video video)
            {
                if (reader.TryGetString(index, out var primaryVersionId))
                {
                    video.PrimaryVersionId = primaryVersionId;
                }
            }

            index++;

            if (HasField(query, ItemFields.DateLastMediaAdded))
            {
                if (item is Folder folder && reader.TryReadDateTime(index, out var dateLastMediaAdded))
                {
                    folder.DateLastMediaAdded = dateLastMediaAdded;
                }

                index++;
            }

            if (reader.TryGetString(index++, out var album))
            {
                item.Album = album;
            }

            if (reader.TryGetSingle(index++, out var criticRating))
            {
                item.CriticRating = criticRating;
            }

            if (reader.TryGetBoolean(index++, out var isVirtualItem))
            {
                item.IsVirtualItem = isVirtualItem;
            }

            if (item is IHasSeries hasSeriesName)
            {
                if (reader.TryGetString(index, out var seriesName))
                {
                    hasSeriesName.SeriesName = seriesName;
                }
            }

            index++;

            if (hasEpisodeAttributes)
            {
                if (item is Episode episode)
                {
                    if (reader.TryGetString(index, out var seasonName))
                    {
                        episode.SeasonName = seasonName;
                    }

                    index++;
                    if (reader.TryGetGuid(index, out var seasonId))
                    {
                        episode.SeasonId = seasonId;
                    }
                }
                else
                {
                    index++;
                }

                index++;
            }

            var hasSeries = item as IHasSeries;
            if (hasSeriesFields)
            {
                if (hasSeries != null)
                {
                    if (reader.TryGetGuid(index, out var seriesId))
                    {
                        hasSeries.SeriesId = seriesId;
                    }
                }

                index++;
            }

            if (HasField(query, ItemFields.PresentationUniqueKey))
            {
                if (reader.TryGetString(index++, out var presentationUniqueKey))
                {
                    item.PresentationUniqueKey = presentationUniqueKey;
                }
            }

            if (HasField(query, ItemFields.InheritedParentalRatingValue))
            {
                if (reader.TryGetInt32(index++, out var parentalRating))
                {
                    item.InheritedParentalRatingValue = parentalRating;
                }
            }

            if (HasField(query, ItemFields.ExternalSeriesId))
            {
                if (reader.TryGetString(index++, out var externalSeriesId))
                {
                    item.ExternalSeriesId = externalSeriesId;
                }
            }

            if (HasField(query, ItemFields.Taglines))
            {
                if (reader.TryGetString(index++, out var tagLine))
                {
                    item.Tagline = tagLine;
                }
            }

            if (item.ProviderIds.Count == 0 && reader.TryGetString(index, out var providerIds))
            {
                DeserializeProviderIds(providerIds, item);
            }

            index++;

            if (query.DtoOptions.EnableImages)
            {
                if (item.ImageInfos.Length == 0 && reader.TryGetString(index, out var imageInfos))
                {
                    item.ImageInfos = DeserializeImages(imageInfos);
                }

                index++;
            }

            if (HasField(query, ItemFields.ProductionLocations))
            {
                if (reader.TryGetString(index++, out var productionLocations))
                {
                    item.ProductionLocations = productionLocations.Split('|', StringSplitOptions.RemoveEmptyEntries);
                }
            }

            if (HasField(query, ItemFields.ExtraIds))
            {
                if (reader.TryGetString(index++, out var extraIds))
                {
                    item.ExtraIds = SplitToGuids(extraIds);
                }
            }

            if (reader.TryGetInt32(index++, out var totalBitrate))
            {
                item.TotalBitrate = totalBitrate;
            }

            if (reader.TryGetString(index++, out var extraTypeString))
            {
                if (Enum.TryParse(extraTypeString, true, out ExtraType extraType))
                {
                    item.ExtraType = extraType;
                }
            }

            if (hasArtistFields)
            {
                if (item is IHasArtist hasArtists && reader.TryGetString(index, out var artists))
                {
                    hasArtists.Artists = artists.Split('|', StringSplitOptions.RemoveEmptyEntries);
                }

                index++;

                if (item is IHasAlbumArtist hasAlbumArtists && reader.TryGetString(index, out var albumArtists))
                {
                    hasAlbumArtists.AlbumArtists = albumArtists.Split('|', StringSplitOptions.RemoveEmptyEntries);
                }

                index++;
            }

            if (reader.TryGetString(index++, out var externalId))
            {
                item.ExternalId = externalId;
            }

            if (HasField(query, ItemFields.SeriesPresentationUniqueKey))
            {
                if (hasSeries != null)
                {
                    if (reader.TryGetString(index, out var seriesPresentationUniqueKey))
                    {
                        hasSeries.SeriesPresentationUniqueKey = seriesPresentationUniqueKey;
                    }
                }

                index++;
            }

            if (enableProgramAttributes)
            {
                if (item is LiveTvProgram program && reader.TryGetString(index, out var showId))
                {
                    program.ShowId = showId;
                }

                index++;
            }

            if (reader.TryGetGuid(index, out var ownerId))
            {
                item.OwnerId = ownerId;
            }

            return item;
        }

        private static Guid[] SplitToGuids(string value)
        {
            var ids = value.Split('|');

            var result = new Guid[ids.Length];

            for (var i = 0; i < result.Length; i++)
            {
                result[i] = new Guid(ids[i]);
            }

            return result;
        }

        /// <inheritdoc />
        public List<ChapterInfo> GetChapters(BaseItem item)
        {
            CheckDisposed();

            using (var connection = GetConnection(true))
            {
                var chapters = new List<ChapterInfo>();

                using (var statement = PrepareStatement(connection, "select StartPositionTicks,Name,ImagePath,ImageDateModified from " + ChaptersTableName + " where ItemId = @ItemId order by ChapterIndex asc"))
                {
                    statement.TryBind("@ItemId", item.Id);

                    foreach (var row in statement.ExecuteQuery())
                    {
                        chapters.Add(GetChapter(row, item));
                    }
                }

                return chapters;
            }
        }

        /// <inheritdoc />
        public ChapterInfo GetChapter(BaseItem item, int index)
        {
            CheckDisposed();

            using (var connection = GetConnection(true))
            {
                using (var statement = PrepareStatement(connection, "select StartPositionTicks,Name,ImagePath,ImageDateModified from " + ChaptersTableName + " where ItemId = @ItemId and ChapterIndex=@ChapterIndex"))
                {
                    statement.TryBind("@ItemId", item.Id);
                    statement.TryBind("@ChapterIndex", index);

                    foreach (var row in statement.ExecuteQuery())
                    {
                        return GetChapter(row, item);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the chapter.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="item">The item.</param>
        /// <returns>ChapterInfo.</returns>
        private ChapterInfo GetChapter(IReadOnlyList<ResultSetValue> reader, BaseItem item)
        {
            var chapter = new ChapterInfo
            {
                StartPositionTicks = reader.GetInt64(0)
            };

            if (reader.TryGetString(1, out var chapterName))
            {
                chapter.Name = chapterName;
            }

            if (reader.TryGetString(2, out var imagePath))
            {
                chapter.ImagePath = imagePath;

                if (!string.IsNullOrEmpty(chapter.ImagePath))
                {
                    try
                    {
                        chapter.ImageTag = _imageProcessor.GetImageCacheTag(item, chapter);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Failed to create image cache tag.");
                    }
                }
            }

            if (reader.TryReadDateTime(3, out var imageDateModified))
            {
                chapter.ImageDateModified = imageDateModified;
            }

            return chapter;
        }

        /// <summary>
        /// Saves the chapters.
        /// </summary>
        /// <param name="id">The item id.</param>
        /// <param name="chapters">The chapters.</param>
        public void SaveChapters(Guid id, IReadOnlyList<ChapterInfo> chapters)
        {
            CheckDisposed();

            if (id.Equals(default))
            {
                throw new ArgumentNullException(nameof(id));
            }

            ArgumentNullException.ThrowIfNull(chapters);

            var idBlob = id.ToByteArray();

            using (var connection = GetConnection())
            {
                connection.RunInTransaction(
                    db =>
                    {
                        // First delete chapters
                        db.Execute("delete from " + ChaptersTableName + " where ItemId=@ItemId", idBlob);

                        InsertChapters(idBlob, chapters, db);
                    },
                    TransactionMode);
            }
        }

        private void InsertChapters(byte[] idBlob, IReadOnlyList<ChapterInfo> chapters, IDatabaseConnection db)
        {
            var startIndex = 0;
            var limit = 100;
            var chapterIndex = 0;

            const string StartInsertText = "insert into " + ChaptersTableName + " (ItemId, ChapterIndex, StartPositionTicks, Name, ImagePath, ImageDateModified) values ";
            var insertText = new StringBuilder(StartInsertText, 256);

            while (startIndex < chapters.Count)
            {
                var endIndex = Math.Min(chapters.Count, startIndex + limit);

                for (var i = startIndex; i < endIndex; i++)
                {
                    insertText.AppendFormat(CultureInfo.InvariantCulture, "(@ItemId, @ChapterIndex{0}, @StartPositionTicks{0}, @Name{0}, @ImagePath{0}, @ImageDateModified{0}),", i.ToString(CultureInfo.InvariantCulture));
                }

                insertText.Length -= 1; // Remove last ,

                using (var statement = PrepareStatement(db, insertText.ToString()))
                {
                    statement.TryBind("@ItemId", idBlob);

                    for (var i = startIndex; i < endIndex; i++)
                    {
                        var index = i.ToString(CultureInfo.InvariantCulture);

                        var chapter = chapters[i];

                        statement.TryBind("@ChapterIndex" + index, chapterIndex);
                        statement.TryBind("@StartPositionTicks" + index, chapter.StartPositionTicks);
                        statement.TryBind("@Name" + index, chapter.Name);
                        statement.TryBind("@ImagePath" + index, chapter.ImagePath);
                        statement.TryBind("@ImageDateModified" + index, chapter.ImageDateModified);

                        chapterIndex++;
                    }

                    statement.Reset();
                    statement.MoveNext();
                }

                startIndex += limit;
                insertText.Length = StartInsertText.Length;
            }
        }

        private static bool EnableJoinUserData(InternalItemsQuery query)
        {
            if (query.User == null)
            {
                return false;
            }

            var sortingFields = new HashSet<string>(query.OrderBy.Select(i => i.OrderBy), StringComparer.OrdinalIgnoreCase);

            return sortingFields.Contains(ItemSortBy.IsFavoriteOrLiked)
                    || sortingFields.Contains(ItemSortBy.IsPlayed)
                    || sortingFields.Contains(ItemSortBy.IsUnplayed)
                    || sortingFields.Contains(ItemSortBy.PlayCount)
                    || sortingFields.Contains(ItemSortBy.DatePlayed)
                    || sortingFields.Contains(ItemSortBy.SeriesDatePlayed)
                    || query.IsFavoriteOrLiked.HasValue
                    || query.IsFavorite.HasValue
                    || query.IsResumable.HasValue
                    || query.IsPlayed.HasValue
                    || query.IsLiked.HasValue;
        }

        private bool HasField(InternalItemsQuery query, ItemFields name)
        {
            switch (name)
            {
                case ItemFields.Tags:
                    return query.DtoOptions.ContainsField(name) || HasProgramAttributes(query);
                case ItemFields.CustomRating:
                case ItemFields.ProductionLocations:
                case ItemFields.Settings:
                case ItemFields.OriginalTitle:
                case ItemFields.Taglines:
                case ItemFields.SortName:
                case ItemFields.Studios:
                case ItemFields.ExtraIds:
                case ItemFields.DateCreated:
                case ItemFields.Overview:
                case ItemFields.Genres:
                case ItemFields.DateLastMediaAdded:
                case ItemFields.PresentationUniqueKey:
                case ItemFields.InheritedParentalRatingValue:
                case ItemFields.ExternalSeriesId:
                case ItemFields.SeriesPresentationUniqueKey:
                case ItemFields.DateLastRefreshed:
                case ItemFields.DateLastSaved:
                    return query.DtoOptions.ContainsField(name);
                case ItemFields.ServiceName:
                    return HasServiceName(query);
                default:
                    return true;
            }
        }

        private bool HasProgramAttributes(InternalItemsQuery query)
        {
            if (query.ParentType != null && _programExcludeParentTypes.Contains(query.ParentType.Value))
            {
                return false;
            }

            if (query.IncludeItemTypes.Length == 0)
            {
                return true;
            }

            return query.IncludeItemTypes.Any(x => _programTypes.Contains(x));
        }

        private bool HasServiceName(InternalItemsQuery query)
        {
            if (query.ParentType != null && _programExcludeParentTypes.Contains(query.ParentType.Value))
            {
                return false;
            }

            if (query.IncludeItemTypes.Length == 0)
            {
                return true;
            }

            return query.IncludeItemTypes.Any(x => _serviceTypes.Contains(x));
        }

        private bool HasStartDate(InternalItemsQuery query)
        {
            if (query.ParentType != null && _programExcludeParentTypes.Contains(query.ParentType.Value))
            {
                return false;
            }

            if (query.IncludeItemTypes.Length == 0)
            {
                return true;
            }

            return query.IncludeItemTypes.Any(x => _startDateTypes.Contains(x));
        }

        private bool HasEpisodeAttributes(InternalItemsQuery query)
        {
            if (query.IncludeItemTypes.Length == 0)
            {
                return true;
            }

            return query.IncludeItemTypes.Contains(BaseItemKind.Episode);
        }

        private bool HasTrailerTypes(InternalItemsQuery query)
        {
            if (query.IncludeItemTypes.Length == 0)
            {
                return true;
            }

            return query.IncludeItemTypes.Contains(BaseItemKind.Trailer);
        }

        private bool HasArtistFields(InternalItemsQuery query)
        {
            if (query.ParentType != null && _artistExcludeParentTypes.Contains(query.ParentType.Value))
            {
                return false;
            }

            if (query.IncludeItemTypes.Length == 0)
            {
                return true;
            }

            return query.IncludeItemTypes.Any(x => _artistsTypes.Contains(x));
        }

        private bool HasSeriesFields(InternalItemsQuery query)
        {
            if (query.ParentType == BaseItemKind.PhotoAlbum)
            {
                return false;
            }

            if (query.IncludeItemTypes.Length == 0)
            {
                return true;
            }

            return query.IncludeItemTypes.Any(x => _seriesTypes.Contains(x));
        }

        private void SetFinalColumnsToSelect(InternalItemsQuery query, List<string> columns)
        {
            foreach (var field in _allItemFields)
            {
                if (!HasField(query, field))
                {
                    switch (field)
                    {
                        case ItemFields.Settings:
                            columns.Remove("IsLocked");
                            columns.Remove("PreferredMetadataCountryCode");
                            columns.Remove("PreferredMetadataLanguage");
                            columns.Remove("LockedFields");
                            break;
                        case ItemFields.ServiceName:
                            columns.Remove("ExternalServiceId");
                            break;
                        case ItemFields.SortName:
                            columns.Remove("ForcedSortName");
                            break;
                        case ItemFields.Taglines:
                            columns.Remove("Tagline");
                            break;
                        case ItemFields.Tags:
                            columns.Remove("Tags");
                            break;
                        case ItemFields.IsHD:
                            // do nothing
                            break;
                        default:
                            columns.Remove(field.ToString());
                            break;
                    }
                }
            }

            if (!HasProgramAttributes(query))
            {
                columns.Remove("IsMovie");
                columns.Remove("IsSeries");
                columns.Remove("EpisodeTitle");
                columns.Remove("IsRepeat");
                columns.Remove("ShowId");
            }

            if (!HasEpisodeAttributes(query))
            {
                columns.Remove("SeasonName");
                columns.Remove("SeasonId");
            }

            if (!HasStartDate(query))
            {
                columns.Remove("StartDate");
            }

            if (!HasTrailerTypes(query))
            {
                columns.Remove("TrailerTypes");
            }

            if (!HasArtistFields(query))
            {
                columns.Remove("AlbumArtists");
                columns.Remove("Artists");
            }

            if (!HasSeriesFields(query))
            {
                columns.Remove("SeriesId");
            }

            if (!HasEpisodeAttributes(query))
            {
                columns.Remove("SeasonName");
                columns.Remove("SeasonId");
            }

            if (!query.DtoOptions.EnableImages)
            {
                columns.Remove("Images");
            }

            if (EnableJoinUserData(query))
            {
                columns.Add("UserDatas.UserId");
                columns.Add("UserDatas.lastPlayedDate");
                columns.Add("UserDatas.playbackPositionTicks");
                columns.Add("UserDatas.playcount");
                columns.Add("UserDatas.isFavorite");
                columns.Add("UserDatas.played");
                columns.Add("UserDatas.rating");
            }

            if (query.SimilarTo != null)
            {
                var item = query.SimilarTo;

                var builder = new StringBuilder();
                builder.Append('(');

                if (string.IsNullOrEmpty(item.OfficialRating))
                {
                    builder.Append("(OfficialRating is null * 10)");
                }
                else
                {
                    builder.Append("(OfficialRating=@ItemOfficialRating * 10)");
                }

                if (item.ProductionYear.HasValue)
                {
                    builder.Append("+(Select Case When Abs(COALESCE(ProductionYear, 0) - @ItemProductionYear) < 10 Then 10 Else 0 End )");
                    builder.Append("+(Select Case When Abs(COALESCE(ProductionYear, 0) - @ItemProductionYear) < 5 Then 5 Else 0 End )");
                }

                // genres, tags, studios, person, year?
                builder.Append("+ (Select count(1) * 10 from ItemValues where ItemId=Guid and CleanValue in (select CleanValue from ItemValues where ItemId=@SimilarItemId))");

                if (item is MusicArtist)
                {
                    // Match albums where the artist is AlbumArtist against other albums.
                    // It is assumed that similar albums => similar artists.
                    builder.Append(
                        @"+ (WITH artistValues AS (
	                            SELECT DISTINCT albumValues.CleanValue
	                            FROM ItemValues albumValues
	                            INNER JOIN ItemValues artistAlbums ON albumValues.ItemId = artistAlbums.ItemId
	                            INNER JOIN TypedBaseItems artistItem ON artistAlbums.CleanValue = artistItem.CleanName AND artistAlbums.TYPE = 1 AND artistItem.Guid = @SimilarItemId
                            ), similarArtist AS (
	                            SELECT albumValues.ItemId
	                            FROM ItemValues albumValues
	                            INNER JOIN ItemValues artistAlbums ON albumValues.ItemId = artistAlbums.ItemId
	                            INNER JOIN TypedBaseItems artistItem ON artistAlbums.CleanValue = artistItem.CleanName AND artistAlbums.TYPE = 1 AND artistItem.Guid = A.Guid
                            ) SELECT COUNT(DISTINCT(CleanValue)) * 10 FROM ItemValues WHERE ItemId IN (SELECT ItemId FROM similarArtist) AND CleanValue IN (SELECT CleanValue FROM artistValues))");
                }

                builder.Append(") as SimilarityScore");

                columns.Add(builder.ToString());

                var oldLen = query.ExcludeItemIds.Length;
                var newLen = oldLen + item.ExtraIds.Length + 1;
                var excludeIds = new Guid[newLen];
                query.ExcludeItemIds.CopyTo(excludeIds, 0);
                excludeIds[oldLen] = item.Id;
                item.ExtraIds.CopyTo(excludeIds, oldLen + 1);

                query.ExcludeItemIds = excludeIds;
                query.ExcludeProviderIds = item.ProviderIds;
            }

            if (!string.IsNullOrEmpty(query.SearchTerm))
            {
                var builder = new StringBuilder();
                builder.Append('(');

                builder.Append("((CleanName like @SearchTermStartsWith or (OriginalTitle not null and OriginalTitle like @SearchTermStartsWith)) * 10)");
                builder.Append("+ ((CleanName = @SearchTermStartsWith COLLATE NOCASE or (OriginalTitle not null and OriginalTitle = @SearchTermStartsWith COLLATE NOCASE)) * 10)");


                if (query.SearchTerm.Length > 1)
                {
                    builder.Append("+ ((CleanName like @SearchTermContains or (OriginalTitle not null and OriginalTitle like @SearchTermContains)) * 10)");
                }

                builder.Append(") as SearchScore");

                columns.Add(builder.ToString());
            }
        }

        private void BindSearchParams(InternalItemsQuery query, IStatement statement)
        {
            var searchTerm = query.SearchTerm;

            if (string.IsNullOrEmpty(searchTerm))
            {
                return;
            }

            searchTerm = FixUnicodeChars(searchTerm);
            searchTerm = GetCleanValue(searchTerm);

            var commandText = statement.SQL;
            if (commandText.Contains("@SearchTermStartsWith", StringComparison.OrdinalIgnoreCase))
            {
                statement.TryBind("@SearchTermStartsWith", searchTerm + "%");
            }

            if (commandText.Contains("@SearchTermContains", StringComparison.OrdinalIgnoreCase))
            {
                statement.TryBind("@SearchTermContains", "%" + searchTerm + "%");
            }
        }

        private void BindSimilarParams(InternalItemsQuery query, IStatement statement)
        {
            var item = query.SimilarTo;

            if (item == null)
            {
                return;
            }

            var commandText = statement.SQL;

            if (commandText.Contains("@ItemOfficialRating", StringComparison.OrdinalIgnoreCase))
            {
                statement.TryBind("@ItemOfficialRating", item.OfficialRating);
            }

            if (commandText.Contains("@ItemProductionYear", StringComparison.OrdinalIgnoreCase))
            {
                statement.TryBind("@ItemProductionYear", item.ProductionYear ?? 0);
            }

            if (commandText.Contains("@SimilarItemId", StringComparison.OrdinalIgnoreCase))
            {
                statement.TryBind("@SimilarItemId", item.Id);
            }
        }

        private string GetJoinUserDataText(InternalItemsQuery query)
        {
            if (!EnableJoinUserData(query))
            {
                return string.Empty;
            }

            return " left join UserDatas on UserDataKey=UserDatas.Key And (UserId=@UserId)";
        }

        private string GetGroupBy(InternalItemsQuery query)
        {
            var enableGroupByPresentationUniqueKey = EnableGroupByPresentationUniqueKey(query);
            if (enableGroupByPresentationUniqueKey && query.GroupBySeriesPresentationUniqueKey)
            {
                return " Group by PresentationUniqueKey, SeriesPresentationUniqueKey";
            }

            if (enableGroupByPresentationUniqueKey)
            {
                return " Group by PresentationUniqueKey";
            }

            if (query.GroupBySeriesPresentationUniqueKey)
            {
                return " Group by SeriesPresentationUniqueKey";
            }

            return string.Empty;
        }

        public int GetCount(InternalItemsQuery query)
        {
            ArgumentNullException.ThrowIfNull(query);

            CheckDisposed();

            var now = DateTime.UtcNow;

            // Hack for right now since we currently don't support filtering out these duplicates within a query
            if (query.Limit.HasValue && query.EnableGroupByMetadataKey)
            {
                query.Limit = query.Limit.Value + 4;
            }

            var columns = new List<string> { "count(distinct PresentationUniqueKey)" };
            SetFinalColumnsToSelect(query, columns);
            var commandTextBuilder = new StringBuilder("select ", 256)
                .AppendJoin(',', columns)
                .Append(FromText)
                .Append(GetJoinUserDataText(query));

            var whereClauses = GetWhereClauses(query, null);
            if (whereClauses.Count != 0)
            {
                commandTextBuilder.Append(" where ")
                    .AppendJoin(" AND ", whereClauses);
            }

            var commandText = commandTextBuilder.ToString();
            int count;
            using (var connection = GetConnection(true))
            {
                using (var statement = PrepareStatement(connection, commandText))
                {
                    if (EnableJoinUserData(query))
                    {
                        statement.TryBind("@UserId", query.User.InternalId);
                    }

                    BindSimilarParams(query, statement);
                    BindSearchParams(query, statement);

                    // Running this again will bind the params
                    GetWhereClauses(query, statement);

                    count = statement.ExecuteQuery().SelectScalarInt().First();
                }
            }

            LogQueryTime("GetCount", commandText, now);
            return count;
        }

        public List<BaseItem> GetItemList(InternalItemsQuery query)
        {
            ArgumentNullException.ThrowIfNull(query);

            CheckDisposed();

            var now = DateTime.UtcNow;

            // Hack for right now since we currently don't support filtering out these duplicates within a query
            if (query.Limit.HasValue && query.EnableGroupByMetadataKey)
            {
                query.Limit = query.Limit.Value + 4;
            }

            var columns = _retrieveItemColumns.ToList();
            SetFinalColumnsToSelect(query, columns);
            var commandTextBuilder = new StringBuilder("select ", 1024)
                .AppendJoin(',', columns)
                .Append(FromText)
                .Append(GetJoinUserDataText(query));

            var whereClauses = GetWhereClauses(query, null);

            if (whereClauses.Count != 0)
            {
                commandTextBuilder.Append(" where ")
                    .AppendJoin(" AND ", whereClauses);
            }

            commandTextBuilder.Append(GetGroupBy(query))
                .Append(GetOrderByText(query));

            if (query.Limit.HasValue || query.StartIndex.HasValue)
            {
                var offset = query.StartIndex ?? 0;

                if (query.Limit.HasValue || offset > 0)
                {
                    commandTextBuilder.Append(" LIMIT ")
                        .Append(query.Limit ?? int.MaxValue);
                }

                if (offset > 0)
                {
                    commandTextBuilder.Append(" OFFSET ")
                        .Append(offset);
                }
            }

            var commandText = commandTextBuilder.ToString();
            var items = new List<BaseItem>();
            using (var connection = GetConnection(true))
            {
                using (var statement = PrepareStatement(connection, commandText))
                {
                    if (EnableJoinUserData(query))
                    {
                        statement.TryBind("@UserId", query.User.InternalId);
                    }

                    BindSimilarParams(query, statement);
                    BindSearchParams(query, statement);

                    // Running this again will bind the params
                    GetWhereClauses(query, statement);

                    var hasEpisodeAttributes = HasEpisodeAttributes(query);
                    var hasServiceName = HasServiceName(query);
                    var hasProgramAttributes = HasProgramAttributes(query);
                    var hasStartDate = HasStartDate(query);
                    var hasTrailerTypes = HasTrailerTypes(query);
                    var hasArtistFields = HasArtistFields(query);
                    var hasSeriesFields = HasSeriesFields(query);

                    foreach (var row in statement.ExecuteQuery())
                    {
                        var item = GetItem(row, query, hasProgramAttributes, hasEpisodeAttributes, hasServiceName, hasStartDate, hasTrailerTypes, hasArtistFields, hasSeriesFields);
                        if (item != null)
                        {
                            items.Add(item);
                        }
                    }
                }

                // Hack for right now since we currently don't support filtering out these duplicates within a query
                if (query.EnableGroupByMetadataKey)
                {
                    var limit = query.Limit ?? int.MaxValue;
                    limit -= 4;
                    var newList = new List<BaseItem>();

                    foreach (var item in items)
                    {
                        AddItem(newList, item);

                        if (newList.Count >= limit)
                        {
                            break;
                        }
                    }

                    items = newList;
                }
            }

            LogQueryTime("GetItemList", commandText, now);

            return items;
        }

        private string FixUnicodeChars(string buffer)
        {
            buffer = buffer.Replace('\u2013', '-'); // en dash
            buffer = buffer.Replace('\u2014', '-'); // em dash
            buffer = buffer.Replace('\u2015', '-'); // horizontal bar
            buffer = buffer.Replace('\u2017', '_'); // double low line
            buffer = buffer.Replace('\u2018', '\''); // left single quotation mark
            buffer = buffer.Replace('\u2019', '\''); // right single quotation mark
            buffer = buffer.Replace('\u201a', ','); // single low-9 quotation mark
            buffer = buffer.Replace('\u201b', '\''); // single high-reversed-9 quotation mark
            buffer = buffer.Replace('\u201c', '\"'); // left double quotation mark
            buffer = buffer.Replace('\u201d', '\"'); // right double quotation mark
            buffer = buffer.Replace('\u201e', '\"'); // double low-9 quotation mark
            buffer = buffer.Replace("\u2026", "...", StringComparison.Ordinal); // horizontal ellipsis
            buffer = buffer.Replace('\u2032', '\''); // prime
            buffer = buffer.Replace('\u2033', '\"'); // double prime
            buffer = buffer.Replace('\u0060', '\''); // grave accent
            return buffer.Replace('\u00B4', '\''); // acute accent
        }

        private void AddItem(List<BaseItem> items, BaseItem newItem)
        {
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];

                foreach (var providerId in newItem.ProviderIds)
                {
                    if (string.Equals(providerId.Key, nameof(MetadataProvider.TmdbCollection), StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (string.Equals(item.GetProviderId(providerId.Key), providerId.Value, StringComparison.Ordinal))
                    {
                        if (newItem.SourceType == SourceType.Library)
                        {
                            items[i] = newItem;
                        }

                        return;
                    }
                }
            }

            items.Add(newItem);
        }

        private void LogQueryTime(string methodName, string commandText, DateTime startDate)
        {
            var elapsed = (DateTime.UtcNow - startDate).TotalMilliseconds;

#if DEBUG
            const int SlowThreshold = 100;
#else
            const int SlowThreshold = 10;
#endif

            if (elapsed >= SlowThreshold)
            {
                Logger.LogDebug(
                    "{Method} query time (slow): {ElapsedMs}ms. Query: {Query}",
                    methodName,
                    elapsed,
                    commandText);
            }
        }

        public QueryResult<BaseItem> GetItems(InternalItemsQuery query)
        {
            ArgumentNullException.ThrowIfNull(query);

            CheckDisposed();

            if (!query.EnableTotalRecordCount || (!query.Limit.HasValue && (query.StartIndex ?? 0) == 0))
            {
                var returnList = GetItemList(query);
                return new QueryResult<BaseItem>(
                    query.StartIndex,
                    returnList.Count,
                    returnList);
            }

            var now = DateTime.UtcNow;

            // Hack for right now since we currently don't support filtering out these duplicates within a query
            if (query.Limit.HasValue && query.EnableGroupByMetadataKey)
            {
                query.Limit = query.Limit.Value + 4;
            }

            var columns = _retrieveItemColumns.ToList();
            SetFinalColumnsToSelect(query, columns);
            var commandTextBuilder = new StringBuilder("select ", 512)
                .AppendJoin(',', columns)
                .Append(FromText)
                .Append(GetJoinUserDataText(query));

            var whereClauses = GetWhereClauses(query, null);

            var whereText = whereClauses.Count == 0 ?
                string.Empty :
                string.Join(" AND ", whereClauses);

            if (!string.IsNullOrEmpty(whereText))
            {
                commandTextBuilder.Append(" where ")
                    .Append(whereText);
            }

            commandTextBuilder.Append(GetGroupBy(query))
                .Append(GetOrderByText(query));

            if (query.Limit.HasValue || query.StartIndex.HasValue)
            {
                var offset = query.StartIndex ?? 0;

                if (query.Limit.HasValue || offset > 0)
                {
                    commandTextBuilder.Append(" LIMIT ")
                        .Append(query.Limit ?? int.MaxValue);
                }

                if (offset > 0)
                {
                    commandTextBuilder.Append(" OFFSET ")
                        .Append(offset);
                }
            }

            var isReturningZeroItems = query.Limit.HasValue && query.Limit <= 0;

            var itemQuery = string.Empty;
            var totalRecordCountQuery = string.Empty;
            if (!isReturningZeroItems)
            {
                itemQuery = commandTextBuilder.ToString();
            }

            if (query.EnableTotalRecordCount)
            {
                commandTextBuilder.Clear();

                commandTextBuilder.Append(" select ");

                List<string> columnsToSelect;
                if (EnableGroupByPresentationUniqueKey(query))
                {
                    columnsToSelect = new List<string> { "count (distinct PresentationUniqueKey)" };
                }
                else if (query.GroupBySeriesPresentationUniqueKey)
                {
                    columnsToSelect = new List<string> { "count (distinct SeriesPresentationUniqueKey)" };
                }
                else
                {
                    columnsToSelect = new List<string> { "count (guid)" };
                }

                SetFinalColumnsToSelect(query, columnsToSelect);

                commandTextBuilder.AppendJoin(',', columnsToSelect)
                    .Append(FromText)
                    .Append(GetJoinUserDataText(query));
                if (!string.IsNullOrEmpty(whereText))
                {
                    commandTextBuilder.Append(" where ")
                        .Append(whereText);
                }

                totalRecordCountQuery = commandTextBuilder.ToString();
            }

            var list = new List<BaseItem>();
            var result = new QueryResult<BaseItem>();
            using (var connection = GetConnection(true))
            {
                connection.RunInTransaction(
                    db =>
                    {
                        var itemQueryStatement = PrepareStatement(db, itemQuery);
                        var totalRecordCountQueryStatement = PrepareStatement(db, totalRecordCountQuery);

                        if (!isReturningZeroItems)
                        {
                            using (var statement = itemQueryStatement)
                            {
                                if (EnableJoinUserData(query))
                                {
                                    statement.TryBind("@UserId", query.User.InternalId);
                                }

                                BindSimilarParams(query, statement);
                                BindSearchParams(query, statement);

                                // Running this again will bind the params
                                GetWhereClauses(query, statement);

                                var hasEpisodeAttributes = HasEpisodeAttributes(query);
                                var hasServiceName = HasServiceName(query);
                                var hasProgramAttributes = HasProgramAttributes(query);
                                var hasStartDate = HasStartDate(query);
                                var hasTrailerTypes = HasTrailerTypes(query);
                                var hasArtistFields = HasArtistFields(query);
                                var hasSeriesFields = HasSeriesFields(query);

                                foreach (var row in statement.ExecuteQuery())
                                {
                                    var item = GetItem(row, query, hasProgramAttributes, hasEpisodeAttributes, hasServiceName, hasStartDate, hasTrailerTypes, hasArtistFields, hasSeriesFields);
                                    if (item != null)
                                    {
                                        list.Add(item);
                                    }
                                }
                            }

                            LogQueryTime("GetItems.ItemQuery", itemQuery, now);
                        }

                        now = DateTime.UtcNow;
                        if (query.EnableTotalRecordCount)
                        {
                            using (var statement = totalRecordCountQueryStatement)
                            {
                                if (EnableJoinUserData(query))
                                {
                                    statement.TryBind("@UserId", query.User.InternalId);
                                }

                                BindSimilarParams(query, statement);
                                BindSearchParams(query, statement);

                                // Running this again will bind the params
                                GetWhereClauses(query, statement);

                                result.TotalRecordCount = statement.ExecuteQuery().SelectScalarInt().First();
                            }

                            LogQueryTime("GetItems.TotalRecordCount", totalRecordCountQuery, now);
                        }
                    },
                    ReadTransactionMode);
            }

            result.StartIndex = query.StartIndex ?? 0;
            result.Items = list;
            return result;
        }

        private string GetOrderByText(InternalItemsQuery query)
        {
            var orderBy = query.OrderBy;
            bool hasSimilar = query.SimilarTo != null;
            bool hasSearch = !string.IsNullOrEmpty(query.SearchTerm);

            if (hasSimilar || hasSearch)
            {
                List<(string, SortOrder)> prepend = new List<(string, SortOrder)>(4);
                if (hasSearch)
                {
                    prepend.Add(("SearchScore", SortOrder.Descending));
                    prepend.Add((ItemSortBy.SortName, SortOrder.Ascending));
                }

                if (hasSimilar)
                {
                    prepend.Add(("SimilarityScore", SortOrder.Descending));
                    prepend.Add((ItemSortBy.Random, SortOrder.Ascending));
                }

                var arr = new (string, SortOrder)[prepend.Count + orderBy.Count];
                prepend.CopyTo(arr, 0);
                orderBy.CopyTo(arr, prepend.Count);
                orderBy = query.OrderBy = arr;
            }
            else if (orderBy.Count == 0)
            {
                return string.Empty;
            }

            return " ORDER BY " + string.Join(',', orderBy.Select(i =>
            {
                var sortBy = MapOrderByField(i.OrderBy, query);
                var sortOrder = i.SortOrder == SortOrder.Ascending ? "ASC" : "DESC";
                return sortBy + " " + sortOrder;
            }));
        }

        private string MapOrderByField(string name, InternalItemsQuery query)
        {
            if (string.Equals(name, ItemSortBy.AirTime, StringComparison.OrdinalIgnoreCase))
            {
                // TODO
                return "SortName";
            }

            if (string.Equals(name, ItemSortBy.Runtime, StringComparison.OrdinalIgnoreCase))
            {
                return "RuntimeTicks";
            }

            if (string.Equals(name, ItemSortBy.Random, StringComparison.OrdinalIgnoreCase))
            {
                return "RANDOM()";
            }

            if (string.Equals(name, ItemSortBy.DatePlayed, StringComparison.OrdinalIgnoreCase))
            {
                if (query.GroupBySeriesPresentationUniqueKey)
                {
                    return "MAX(LastPlayedDate)";
                }

                return "LastPlayedDate";
            }

            if (string.Equals(name, ItemSortBy.PlayCount, StringComparison.OrdinalIgnoreCase))
            {
                return ItemSortBy.PlayCount;
            }

            if (string.Equals(name, ItemSortBy.IsFavoriteOrLiked, StringComparison.OrdinalIgnoreCase))
            {
                return "(Select Case When IsFavorite is null Then 0 Else IsFavorite End )";
            }

            if (string.Equals(name, ItemSortBy.IsFolder, StringComparison.OrdinalIgnoreCase))
            {
                return ItemSortBy.IsFolder;
            }

            if (string.Equals(name, ItemSortBy.IsPlayed, StringComparison.OrdinalIgnoreCase))
            {
                return "played";
            }

            if (string.Equals(name, ItemSortBy.IsUnplayed, StringComparison.OrdinalIgnoreCase))
            {
                return "played";
            }

            if (string.Equals(name, ItemSortBy.DateLastContentAdded, StringComparison.OrdinalIgnoreCase))
            {
                return "DateLastMediaAdded";
            }

            if (string.Equals(name, ItemSortBy.Artist, StringComparison.OrdinalIgnoreCase))
            {
                return "(select CleanValue from ItemValues where ItemId=Guid and Type=0 LIMIT 1)";
            }

            if (string.Equals(name, ItemSortBy.AlbumArtist, StringComparison.OrdinalIgnoreCase))
            {
                return "(select CleanValue from ItemValues where ItemId=Guid and Type=1 LIMIT 1)";
            }

            if (string.Equals(name, ItemSortBy.OfficialRating, StringComparison.OrdinalIgnoreCase))
            {
                return "InheritedParentalRatingValue";
            }

            if (string.Equals(name, ItemSortBy.Studio, StringComparison.OrdinalIgnoreCase))
            {
                return "(select CleanValue from ItemValues where ItemId=Guid and Type=3 LIMIT 1)";
            }

            if (string.Equals(name, ItemSortBy.SeriesDatePlayed, StringComparison.OrdinalIgnoreCase))
            {
                return "(Select MAX(LastPlayedDate) from TypedBaseItems B" + GetJoinUserDataText(query) + " where Played=1 and B.SeriesPresentationUniqueKey=A.PresentationUniqueKey)";
            }

            if (string.Equals(name, ItemSortBy.SeriesSortName, StringComparison.OrdinalIgnoreCase))
            {
                return "SeriesName";
            }

            if (string.Equals(name, ItemSortBy.AiredEpisodeOrder, StringComparison.OrdinalIgnoreCase))
            {
                return ItemSortBy.AiredEpisodeOrder;
            }

            if (string.Equals(name, ItemSortBy.Album, StringComparison.OrdinalIgnoreCase))
            {
                return ItemSortBy.Album;
            }

            if (string.Equals(name, ItemSortBy.DateCreated, StringComparison.OrdinalIgnoreCase))
            {
                return ItemSortBy.DateCreated;
            }

            if (string.Equals(name, ItemSortBy.PremiereDate, StringComparison.OrdinalIgnoreCase))
            {
                return ItemSortBy.PremiereDate;
            }

            if (string.Equals(name, ItemSortBy.StartDate, StringComparison.OrdinalIgnoreCase))
            {
                return ItemSortBy.StartDate;
            }

            if (string.Equals(name, ItemSortBy.Name, StringComparison.OrdinalIgnoreCase))
            {
                return ItemSortBy.Name;
            }

            if (string.Equals(name, ItemSortBy.CommunityRating, StringComparison.OrdinalIgnoreCase))
            {
                return ItemSortBy.CommunityRating;
            }

            if (string.Equals(name, ItemSortBy.ProductionYear, StringComparison.OrdinalIgnoreCase))
            {
                return ItemSortBy.ProductionYear;
            }

            if (string.Equals(name, ItemSortBy.CriticRating, StringComparison.OrdinalIgnoreCase))
            {
                return ItemSortBy.CriticRating;
            }

            if (string.Equals(name, ItemSortBy.VideoBitRate, StringComparison.OrdinalIgnoreCase))
            {
                return ItemSortBy.VideoBitRate;
            }

            if (string.Equals(name, ItemSortBy.ParentIndexNumber, StringComparison.OrdinalIgnoreCase))
            {
                return ItemSortBy.ParentIndexNumber;
            }

            if (string.Equals(name, ItemSortBy.IndexNumber, StringComparison.OrdinalIgnoreCase))
            {
                return ItemSortBy.IndexNumber;
            }

            if (string.Equals(name, ItemSortBy.SimilarityScore, StringComparison.OrdinalIgnoreCase))
            {
                return ItemSortBy.SimilarityScore;
            }

            if (string.Equals(name, ItemSortBy.SearchScore, StringComparison.OrdinalIgnoreCase))
            {
                return ItemSortBy.SearchScore;
            }

            // Unknown SortBy, just sort by the SortName.
            return ItemSortBy.SortName;
        }

        public List<Guid> GetItemIdsList(InternalItemsQuery query)
        {
            ArgumentNullException.ThrowIfNull(query);

            CheckDisposed();

            var now = DateTime.UtcNow;

            var columns = new List<string> { "guid" };
            SetFinalColumnsToSelect(query, columns);
            var commandTextBuilder = new StringBuilder("select ", 256)
                .AppendJoin(',', columns)
                .Append(FromText)
                .Append(GetJoinUserDataText(query));

            var whereClauses = GetWhereClauses(query, null);
            if (whereClauses.Count != 0)
            {
                commandTextBuilder.Append(" where ")
                    .AppendJoin(" AND ", whereClauses);
            }

            commandTextBuilder.Append(GetGroupBy(query))
                .Append(GetOrderByText(query));

            if (query.Limit.HasValue || query.StartIndex.HasValue)
            {
                var offset = query.StartIndex ?? 0;

                if (query.Limit.HasValue || offset > 0)
                {
                    commandTextBuilder.Append(" LIMIT ")
                        .Append(query.Limit ?? int.MaxValue);
                }

                if (offset > 0)
                {
                    commandTextBuilder.Append(" OFFSET ")
                        .Append(offset);
                }
            }

            var commandText = commandTextBuilder.ToString();
            var list = new List<Guid>();
            using (var connection = GetConnection(true))
            {
                using (var statement = PrepareStatement(connection, commandText))
                {
                    if (EnableJoinUserData(query))
                    {
                        statement.TryBind("@UserId", query.User.InternalId);
                    }

                    BindSimilarParams(query, statement);
                    BindSearchParams(query, statement);

                    // Running this again will bind the params
                    GetWhereClauses(query, statement);

                    foreach (var row in statement.ExecuteQuery())
                    {
                        list.Add(row[0].ReadGuidFromBlob());
                    }
                }
            }

            LogQueryTime("GetItemList", commandText, now);
            return list;
        }

        private bool IsAlphaNumeric(string str)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return false;
            }

            for (int i = 0; i < str.Length; i++)
            {
                if (!char.IsLetter(str[i]) && !char.IsNumber(str[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsValidMediaType(string value)
        {
            return IsAlphaNumeric(value);
        }

        private bool IsValidPersonType(string value)
        {
            return IsAlphaNumeric(value);
        }

        private List<string> GetWhereClauses(InternalItemsQuery query, IStatement statement)
        {
            if (query.IsResumable ?? false)
            {
                query.IsVirtualItem = false;
            }

            var minWidth = query.MinWidth;
            var maxWidth = query.MaxWidth;

            if (query.IsHD.HasValue)
            {
                const int Threshold = 1200;
                if (query.IsHD.Value)
                {
                    minWidth = Threshold;
                }
                else
                {
                    maxWidth = Threshold - 1;
                }
            }

            if (query.Is4K.HasValue)
            {
                const int Threshold = 3800;
                if (query.Is4K.Value)
                {
                    minWidth = Threshold;
                }
                else
                {
                    maxWidth = Threshold - 1;
                }
            }

            var whereClauses = new List<string>();

            if (minWidth.HasValue)
            {
                whereClauses.Add("Width>=@MinWidth");
                statement?.TryBind("@MinWidth", minWidth);
            }

            if (query.MinHeight.HasValue)
            {
                whereClauses.Add("Height>=@MinHeight");
                statement?.TryBind("@MinHeight", query.MinHeight);
            }

            if (maxWidth.HasValue)
            {
                whereClauses.Add("Width<=@MaxWidth");
                statement?.TryBind("@MaxWidth", maxWidth);
            }

            if (query.MaxHeight.HasValue)
            {
                whereClauses.Add("Height<=@MaxHeight");
                statement?.TryBind("@MaxHeight", query.MaxHeight);
            }

            if (query.IsLocked.HasValue)
            {
                whereClauses.Add("IsLocked=@IsLocked");
                statement?.TryBind("@IsLocked", query.IsLocked);
            }

            var tags = query.Tags.ToList();
            var excludeTags = query.ExcludeTags.ToList();

            if (query.IsMovie == true)
            {
                if (query.IncludeItemTypes.Length == 0
                    || query.IncludeItemTypes.Contains(BaseItemKind.Movie)
                    || query.IncludeItemTypes.Contains(BaseItemKind.Trailer))
                {
                    whereClauses.Add("(IsMovie is null OR IsMovie=@IsMovie)");
                }
                else
                {
                    whereClauses.Add("IsMovie=@IsMovie");
                }

                statement?.TryBind("@IsMovie", true);
            }
            else if (query.IsMovie.HasValue)
            {
                whereClauses.Add("IsMovie=@IsMovie");
                statement?.TryBind("@IsMovie", query.IsMovie);
            }

            if (query.IsSeries.HasValue)
            {
                whereClauses.Add("IsSeries=@IsSeries");
                statement?.TryBind("@IsSeries", query.IsSeries);
            }

            if (query.IsSports.HasValue)
            {
                if (query.IsSports.Value)
                {
                    tags.Add("Sports");
                }
                else
                {
                    excludeTags.Add("Sports");
                }
            }

            if (query.IsNews.HasValue)
            {
                if (query.IsNews.Value)
                {
                    tags.Add("News");
                }
                else
                {
                    excludeTags.Add("News");
                }
            }

            if (query.IsKids.HasValue)
            {
                if (query.IsKids.Value)
                {
                    tags.Add("Kids");
                }
                else
                {
                    excludeTags.Add("Kids");
                }
            }

            if (query.SimilarTo != null && query.MinSimilarityScore > 0)
            {
                whereClauses.Add("SimilarityScore > " + (query.MinSimilarityScore - 1).ToString(CultureInfo.InvariantCulture));
            }

            if (!string.IsNullOrEmpty(query.SearchTerm))
            {
                whereClauses.Add("SearchScore > 0");
            }

            if (query.IsFolder.HasValue)
            {
                whereClauses.Add("IsFolder=@IsFolder");
                statement?.TryBind("@IsFolder", query.IsFolder);
            }

            var includeTypes = query.IncludeItemTypes;
            // Only specify excluded types if no included types are specified
            if (query.IncludeItemTypes.Length == 0)
            {
                var excludeTypes = query.ExcludeItemTypes;
                if (excludeTypes.Length == 1)
                {
                    if (_baseItemKindNames.TryGetValue(excludeTypes[0], out var excludeTypeName))
                    {
                        whereClauses.Add("type<>@type");
                        statement?.TryBind("@type", excludeTypeName);
                    }
                    else
                    {
                        Logger.LogWarning("Undefined BaseItemKind to Type mapping: {BaseItemKind}", excludeTypes[0]);
                    }
                }
                else if (excludeTypes.Length > 1)
                {
                    var whereBuilder = new StringBuilder("type not in (");
                    foreach (var excludeType in excludeTypes)
                    {
                        if (_baseItemKindNames.TryGetValue(excludeType, out var baseItemKindName))
                        {
                            whereBuilder
                                .Append('\'')
                                .Append(baseItemKindName)
                                .Append("',");
                        }
                        else
                        {
                            Logger.LogWarning("Undefined BaseItemKind to Type mapping: {BaseItemKind}", excludeType);
                        }
                    }

                    // Remove trailing comma.
                    whereBuilder.Length--;
                    whereBuilder.Append(')');
                    whereClauses.Add(whereBuilder.ToString());
                }
            }
            else if (includeTypes.Length == 1)
            {
                if (_baseItemKindNames.TryGetValue(includeTypes[0], out var includeTypeName))
                {
                    whereClauses.Add("type=@type");
                    statement?.TryBind("@type", includeTypeName);
                }
                else
                {
                    Logger.LogWarning("Undefined BaseItemKind to Type mapping: {BaseItemKind}", includeTypes[0]);
                }
            }
            else if (includeTypes.Length > 1)
            {
                var whereBuilder = new StringBuilder("type in (");
                foreach (var includeType in includeTypes)
                {
                    if (_baseItemKindNames.TryGetValue(includeType, out var baseItemKindName))
                    {
                        whereBuilder
                            .Append('\'')
                            .Append(baseItemKindName)
                            .Append("',");
                    }
                    else
                    {
                        Logger.LogWarning("Undefined BaseItemKind to Type mapping: {BaseItemKind}", includeType);
                    }
                }

                // Remove trailing comma.
                whereBuilder.Length--;
                whereBuilder.Append(')');
                whereClauses.Add(whereBuilder.ToString());
            }

            if (query.ChannelIds.Count == 1)
            {
                whereClauses.Add("ChannelId=@ChannelId");
                statement?.TryBind("@ChannelId", query.ChannelIds[0].ToString("N", CultureInfo.InvariantCulture));
            }
            else if (query.ChannelIds.Count > 1)
            {
                var inClause = string.Join(',', query.ChannelIds.Select(i => "'" + i.ToString("N", CultureInfo.InvariantCulture) + "'"));
                whereClauses.Add($"ChannelId in ({inClause})");
            }

            if (!query.ParentId.Equals(default))
            {
                whereClauses.Add("ParentId=@ParentId");
                statement?.TryBind("@ParentId", query.ParentId);
            }

            if (!string.IsNullOrWhiteSpace(query.Path))
            {
                whereClauses.Add("Path=@Path");
                statement?.TryBind("@Path", GetPathToSave(query.Path));
            }

            if (!string.IsNullOrWhiteSpace(query.PresentationUniqueKey))
            {
                whereClauses.Add("PresentationUniqueKey=@PresentationUniqueKey");
                statement?.TryBind("@PresentationUniqueKey", query.PresentationUniqueKey);
            }

            if (query.MinCommunityRating.HasValue)
            {
                whereClauses.Add("CommunityRating>=@MinCommunityRating");
                statement?.TryBind("@MinCommunityRating", query.MinCommunityRating.Value);
            }

            if (query.MinIndexNumber.HasValue)
            {
                whereClauses.Add("IndexNumber>=@MinIndexNumber");
                statement?.TryBind("@MinIndexNumber", query.MinIndexNumber.Value);
            }

            if (query.MinParentAndIndexNumber.HasValue)
            {
                whereClauses.Add("((ParentIndexNumber=@MinParentAndIndexNumberParent and IndexNumber>=@MinParentAndIndexNumberIndex) or ParentIndexNumber>@MinParentAndIndexNumberParent)");
                statement?.TryBind("@MinParentAndIndexNumberParent", query.MinParentAndIndexNumber.Value.ParentIndexNumber);
                statement?.TryBind("@MinParentAndIndexNumberIndex", query.MinParentAndIndexNumber.Value.IndexNumber);
            }

            if (query.MinDateCreated.HasValue)
            {
                whereClauses.Add("DateCreated>=@MinDateCreated");
                statement?.TryBind("@MinDateCreated", query.MinDateCreated.Value);
            }

            if (query.MinDateLastSaved.HasValue)
            {
                whereClauses.Add("(DateLastSaved not null and DateLastSaved>=@MinDateLastSavedForUser)");
                statement?.TryBind("@MinDateLastSaved", query.MinDateLastSaved.Value);
            }

            if (query.MinDateLastSavedForUser.HasValue)
            {
                whereClauses.Add("(DateLastSaved not null and DateLastSaved>=@MinDateLastSavedForUser)");
                statement?.TryBind("@MinDateLastSavedForUser", query.MinDateLastSavedForUser.Value);
            }

            if (query.IndexNumber.HasValue)
            {
                whereClauses.Add("IndexNumber=@IndexNumber");
                statement?.TryBind("@IndexNumber", query.IndexNumber.Value);
            }

            if (query.ParentIndexNumber.HasValue)
            {
                whereClauses.Add("ParentIndexNumber=@ParentIndexNumber");
                statement?.TryBind("@ParentIndexNumber", query.ParentIndexNumber.Value);
            }

            if (query.ParentIndexNumberNotEquals.HasValue)
            {
                whereClauses.Add("(ParentIndexNumber<>@ParentIndexNumberNotEquals or ParentIndexNumber is null)");
                statement?.TryBind("@ParentIndexNumberNotEquals", query.ParentIndexNumberNotEquals.Value);
            }

            var minEndDate = query.MinEndDate;
            var maxEndDate = query.MaxEndDate;

            if (query.HasAired.HasValue)
            {
                if (query.HasAired.Value)
                {
                    maxEndDate = DateTime.UtcNow;
                }
                else
                {
                    minEndDate = DateTime.UtcNow;
                }
            }

            if (minEndDate.HasValue)
            {
                whereClauses.Add("EndDate>=@MinEndDate");
                statement?.TryBind("@MinEndDate", minEndDate.Value);
            }

            if (maxEndDate.HasValue)
            {
                whereClauses.Add("EndDate<=@MaxEndDate");
                statement?.TryBind("@MaxEndDate", maxEndDate.Value);
            }

            if (query.MinStartDate.HasValue)
            {
                whereClauses.Add("StartDate>=@MinStartDate");
                statement?.TryBind("@MinStartDate", query.MinStartDate.Value);
            }

            if (query.MaxStartDate.HasValue)
            {
                whereClauses.Add("StartDate<=@MaxStartDate");
                statement?.TryBind("@MaxStartDate", query.MaxStartDate.Value);
            }

            if (query.MinPremiereDate.HasValue)
            {
                whereClauses.Add("PremiereDate>=@MinPremiereDate");
                statement?.TryBind("@MinPremiereDate", query.MinPremiereDate.Value);
            }

            if (query.MaxPremiereDate.HasValue)
            {
                whereClauses.Add("PremiereDate<=@MaxPremiereDate");
                statement?.TryBind("@MaxPremiereDate", query.MaxPremiereDate.Value);
            }

            StringBuilder clauseBuilder = new StringBuilder();
            const string Or = " OR ";

            var trailerTypes = query.TrailerTypes;
            int trailerTypesLen = trailerTypes.Length;
            if (trailerTypesLen > 0)
            {
                clauseBuilder.Append('(');

                for (int i = 0; i < trailerTypesLen; i++)
                {
                    var paramName = "@TrailerTypes" + i;
                    clauseBuilder.Append("TrailerTypes like ")
                        .Append(paramName)
                        .Append(Or);
                    statement?.TryBind(paramName, "%" + trailerTypes[i] + "%");
                }

                // Remove last " OR "
                clauseBuilder.Length -= Or.Length;
                clauseBuilder.Append(')');

                whereClauses.Add(clauseBuilder.ToString());

                clauseBuilder.Length = 0;
            }

            if (query.IsAiring.HasValue)
            {
                if (query.IsAiring.Value)
                {
                    whereClauses.Add("StartDate<=@MaxStartDate");
                    statement?.TryBind("@MaxStartDate", DateTime.UtcNow);

                    whereClauses.Add("EndDate>=@MinEndDate");
                    statement?.TryBind("@MinEndDate", DateTime.UtcNow);
                }
                else
                {
                    whereClauses.Add("(StartDate>@IsAiringDate OR EndDate < @IsAiringDate)");
                    statement?.TryBind("@IsAiringDate", DateTime.UtcNow);
                }
            }

            int personIdsLen = query.PersonIds.Length;
            if (personIdsLen > 0)
            {
                // TODO: Should this query with CleanName ?

                clauseBuilder.Append('(');

                Span<byte> idBytes = stackalloc byte[16];
                for (int i = 0; i < personIdsLen; i++)
                {
                    string paramName = "@PersonId" + i;
                    clauseBuilder.Append("(guid in (select itemid from People where Name = (select Name from TypedBaseItems where guid=")
                        .Append(paramName)
                        .Append("))) OR ");

                    if (statement != null)
                    {
                        query.PersonIds[i].TryWriteBytes(idBytes);
                        statement.TryBind(paramName, idBytes);
                    }
                }

                // Remove last " OR "
                clauseBuilder.Length -= Or.Length;
                clauseBuilder.Append(')');

                whereClauses.Add(clauseBuilder.ToString());

                clauseBuilder.Length = 0;
            }

            if (!string.IsNullOrWhiteSpace(query.Person))
            {
                whereClauses.Add("Guid in (select ItemId from People where Name=@PersonName)");
                statement?.TryBind("@PersonName", query.Person);
            }

            if (!string.IsNullOrWhiteSpace(query.MinSortName))
            {
                whereClauses.Add("SortName>=@MinSortName");
                statement?.TryBind("@MinSortName", query.MinSortName);
            }

            if (!string.IsNullOrWhiteSpace(query.ExternalSeriesId))
            {
                whereClauses.Add("ExternalSeriesId=@ExternalSeriesId");
                statement?.TryBind("@ExternalSeriesId", query.ExternalSeriesId);
            }

            if (!string.IsNullOrWhiteSpace(query.ExternalId))
            {
                whereClauses.Add("ExternalId=@ExternalId");
                statement?.TryBind("@ExternalId", query.ExternalId);
            }

            if (!string.IsNullOrWhiteSpace(query.Name))
            {
                whereClauses.Add("CleanName=@Name");
                statement?.TryBind("@Name", GetCleanValue(query.Name));
            }

            // These are the same, for now
            var nameContains = query.NameContains;
            if (!string.IsNullOrWhiteSpace(nameContains))
            {
                whereClauses.Add("(CleanName like @NameContains or OriginalTitle like @NameContains)");
                if (statement != null)
                {
                    nameContains = FixUnicodeChars(nameContains);

                    statement.TryBind("@NameContains", "%" + GetCleanValue(nameContains) + "%");
                }
            }

            if (!string.IsNullOrWhiteSpace(query.NameStartsWith))
            {
                whereClauses.Add("SortName like @NameStartsWith");
                statement?.TryBind("@NameStartsWith", query.NameStartsWith + "%");
            }

            if (!string.IsNullOrWhiteSpace(query.NameStartsWithOrGreater))
            {
                whereClauses.Add("SortName >= @NameStartsWithOrGreater");
                // lowercase this because SortName is stored as lowercase
                statement?.TryBind("@NameStartsWithOrGreater", query.NameStartsWithOrGreater.ToLowerInvariant());
            }

            if (!string.IsNullOrWhiteSpace(query.NameLessThan))
            {
                whereClauses.Add("SortName < @NameLessThan");
                // lowercase this because SortName is stored as lowercase
                statement?.TryBind("@NameLessThan", query.NameLessThan.ToLowerInvariant());
            }

            if (query.ImageTypes.Length > 0)
            {
                foreach (var requiredImage in query.ImageTypes)
                {
                    whereClauses.Add("Images like '%" + requiredImage + "%'");
                }
            }

            if (query.IsLiked.HasValue)
            {
                if (query.IsLiked.Value)
                {
                    whereClauses.Add("rating>=@UserRating");
                    statement?.TryBind("@UserRating", UserItemData.MinLikeValue);
                }
                else
                {
                    whereClauses.Add("(rating is null or rating<@UserRating)");
                    statement?.TryBind("@UserRating", UserItemData.MinLikeValue);
                }
            }

            if (query.IsFavoriteOrLiked.HasValue)
            {
                if (query.IsFavoriteOrLiked.Value)
                {
                    whereClauses.Add("IsFavorite=@IsFavoriteOrLiked");
                }
                else
                {
                    whereClauses.Add("(IsFavorite is null or IsFavorite=@IsFavoriteOrLiked)");
                }

                statement?.TryBind("@IsFavoriteOrLiked", query.IsFavoriteOrLiked.Value);
            }

            if (query.IsFavorite.HasValue)
            {
                if (query.IsFavorite.Value)
                {
                    whereClauses.Add("IsFavorite=@IsFavorite");
                }
                else
                {
                    whereClauses.Add("(IsFavorite is null or IsFavorite=@IsFavorite)");
                }

                statement?.TryBind("@IsFavorite", query.IsFavorite.Value);
            }

            if (EnableJoinUserData(query))
            {
                if (query.IsPlayed.HasValue)
                {
                    // We should probably figure this out for all folders, but for right now, this is the only place where we need it
                    if (query.IncludeItemTypes.Length == 1 && query.IncludeItemTypes[0] == BaseItemKind.Series)
                    {
                        if (query.IsPlayed.Value)
                        {
                            whereClauses.Add("PresentationUniqueKey not in (select S.SeriesPresentationUniqueKey from TypedBaseitems S left join UserDatas UD on S.UserDataKey=UD.Key And UD.UserId=@UserId where Coalesce(UD.Played, 0)=0 and S.IsFolder=0 and S.IsVirtualItem=0 and S.SeriesPresentationUniqueKey not null)");
                        }
                        else
                        {
                            whereClauses.Add("PresentationUniqueKey in (select S.SeriesPresentationUniqueKey from TypedBaseitems S left join UserDatas UD on S.UserDataKey=UD.Key And UD.UserId=@UserId where Coalesce(UD.Played, 0)=0 and S.IsFolder=0 and S.IsVirtualItem=0 and S.SeriesPresentationUniqueKey not null)");
                        }
                    }
                    else
                    {
                        if (query.IsPlayed.Value)
                        {
                            whereClauses.Add("(played=@IsPlayed)");
                        }
                        else
                        {
                            whereClauses.Add("(played is null or played=@IsPlayed)");
                        }

                        statement?.TryBind("@IsPlayed", query.IsPlayed.Value);
                    }
                }
            }

            if (query.IsResumable.HasValue)
            {
                if (query.IsResumable.Value)
                {
                    whereClauses.Add("playbackPositionTicks > 0");
                }
                else
                {
                    whereClauses.Add("(playbackPositionTicks is null or playbackPositionTicks = 0)");
                }
            }

            if (query.ArtistIds.Length > 0)
            {
                var clauses = new List<string>();
                var index = 0;
                foreach (var artistId in query.ArtistIds)
                {
                    var paramName = "@ArtistIds" + index;

                    clauses.Add("(guid in (select itemid from ItemValues where CleanValue = (select CleanName from TypedBaseItems where guid=" + paramName + ") and Type<=1))");
                    if (statement != null)
                    {
                        statement.TryBind(paramName, artistId);
                    }

                    index++;
                }

                var clause = "(" + string.Join(" OR ", clauses) + ")";
                whereClauses.Add(clause);
            }

            if (query.AlbumArtistIds.Length > 0)
            {
                var clauses = new List<string>();
                var index = 0;
                foreach (var artistId in query.AlbumArtistIds)
                {
                    var paramName = "@ArtistIds" + index;

                    clauses.Add("(guid in (select itemid from ItemValues where CleanValue = (select CleanName from TypedBaseItems where guid=" + paramName + ") and Type=1))");
                    if (statement != null)
                    {
                        statement.TryBind(paramName, artistId);
                    }

                    index++;
                }

                var clause = "(" + string.Join(" OR ", clauses) + ")";
                whereClauses.Add(clause);
            }

            if (query.ContributingArtistIds.Length > 0)
            {
                var clauses = new List<string>();
                var index = 0;
                foreach (var artistId in query.ContributingArtistIds)
                {
                    var paramName = "@ArtistIds" + index;

                    clauses.Add("((select CleanName from TypedBaseItems where guid=" + paramName + ") in (select CleanValue from ItemValues where ItemId=Guid and Type=0) AND (select CleanName from TypedBaseItems where guid=" + paramName + ") not in (select CleanValue from ItemValues where ItemId=Guid and Type=1))");
                    if (statement != null)
                    {
                        statement.TryBind(paramName, artistId);
                    }

                    index++;
                }

                var clause = "(" + string.Join(" OR ", clauses) + ")";
                whereClauses.Add(clause);
            }

            if (query.AlbumIds.Length > 0)
            {
                var clauses = new List<string>();
                var index = 0;
                foreach (var albumId in query.AlbumIds)
                {
                    var paramName = "@AlbumIds" + index;

                    clauses.Add("Album in (select Name from typedbaseitems where guid=" + paramName + ")");
                    if (statement != null)
                    {
                        statement.TryBind(paramName, albumId);
                    }

                    index++;
                }

                var clause = "(" + string.Join(" OR ", clauses) + ")";
                whereClauses.Add(clause);
            }

            if (query.ExcludeArtistIds.Length > 0)
            {
                var clauses = new List<string>();
                var index = 0;
                foreach (var artistId in query.ExcludeArtistIds)
                {
                    var paramName = "@ExcludeArtistId" + index;

                    clauses.Add("(guid not in (select itemid from ItemValues where CleanValue = (select CleanName from TypedBaseItems where guid=" + paramName + ") and Type<=1))");
                    if (statement != null)
                    {
                        statement.TryBind(paramName, artistId);
                    }

                    index++;
                }

                var clause = "(" + string.Join(" OR ", clauses) + ")";
                whereClauses.Add(clause);
            }

            if (query.GenreIds.Count > 0)
            {
                var clauses = new List<string>();
                var index = 0;
                foreach (var genreId in query.GenreIds)
                {
                    var paramName = "@GenreId" + index;

                    clauses.Add("(guid in (select itemid from ItemValues where CleanValue = (select CleanName from TypedBaseItems where guid=" + paramName + ") and Type=2))");
                    if (statement != null)
                    {
                        statement.TryBind(paramName, genreId);
                    }

                    index++;
                }

                var clause = "(" + string.Join(" OR ", clauses) + ")";
                whereClauses.Add(clause);
            }

            if (query.Genres.Count > 0)
            {
                var clauses = new List<string>();
                var index = 0;
                foreach (var item in query.Genres)
                {
                    clauses.Add("@Genre" + index + " in (select CleanValue from ItemValues where ItemId=Guid and Type=2)");
                    if (statement != null)
                    {
                        statement.TryBind("@Genre" + index, GetCleanValue(item));
                    }

                    index++;
                }

                var clause = "(" + string.Join(" OR ", clauses) + ")";
                whereClauses.Add(clause);
            }

            if (tags.Count > 0)
            {
                var clauses = new List<string>();
                var index = 0;
                foreach (var item in tags)
                {
                    clauses.Add("@Tag" + index + " in (select CleanValue from ItemValues where ItemId=Guid and Type=4)");
                    if (statement != null)
                    {
                        statement.TryBind("@Tag" + index, GetCleanValue(item));
                    }

                    index++;
                }

                var clause = "(" + string.Join(" OR ", clauses) + ")";
                whereClauses.Add(clause);
            }

            if (excludeTags.Count > 0)
            {
                var clauses = new List<string>();
                var index = 0;
                foreach (var item in excludeTags)
                {
                    clauses.Add("@ExcludeTag" + index + " not in (select CleanValue from ItemValues where ItemId=Guid and Type=4)");
                    if (statement != null)
                    {
                        statement.TryBind("@ExcludeTag" + index, GetCleanValue(item));
                    }

                    index++;
                }

                var clause = "(" + string.Join(" OR ", clauses) + ")";
                whereClauses.Add(clause);
            }

            if (query.StudioIds.Length > 0)
            {
                var clauses = new List<string>();
                var index = 0;
                foreach (var studioId in query.StudioIds)
                {
                    var paramName = "@StudioId" + index;

                    clauses.Add("(guid in (select itemid from ItemValues where CleanValue = (select CleanName from TypedBaseItems where guid=" + paramName + ") and Type=3))");

                    if (statement != null)
                    {
                        statement.TryBind(paramName, studioId);
                    }

                    index++;
                }

                var clause = "(" + string.Join(" OR ", clauses) + ")";
                whereClauses.Add(clause);
            }

            if (query.OfficialRatings.Length > 0)
            {
                var clauses = new List<string>();
                var index = 0;
                foreach (var item in query.OfficialRatings)
                {
                    clauses.Add("OfficialRating=@OfficialRating" + index);
                    if (statement != null)
                    {
                        statement.TryBind("@OfficialRating" + index, item);
                    }

                    index++;
                }

                var clause = "(" + string.Join(" OR ", clauses) + ")";
                whereClauses.Add(clause);
            }

            if (query.MinParentalRating.HasValue)
            {
                whereClauses.Add("InheritedParentalRatingValue>=@MinParentalRating");
                if (statement != null)
                {
                    statement.TryBind("@MinParentalRating", query.MinParentalRating.Value);
                }
            }

            if (query.MaxParentalRating.HasValue)
            {
                whereClauses.Add("InheritedParentalRatingValue<=@MaxParentalRating");
                if (statement != null)
                {
                    statement.TryBind("@MaxParentalRating", query.MaxParentalRating.Value);
                }
            }

            if (query.HasParentalRating.HasValue)
            {
                if (query.HasParentalRating.Value)
                {
                    whereClauses.Add("InheritedParentalRatingValue > 0");
                }
                else
                {
                    whereClauses.Add("InheritedParentalRatingValue = 0");
                }
            }

            if (query.HasOfficialRating.HasValue)
            {
                if (query.HasOfficialRating.Value)
                {
                    whereClauses.Add("(OfficialRating not null AND OfficialRating<>'')");
                }
                else
                {
                    whereClauses.Add("(OfficialRating is null OR OfficialRating='')");
                }
            }

            if (query.HasOverview.HasValue)
            {
                if (query.HasOverview.Value)
                {
                    whereClauses.Add("(Overview not null AND Overview<>'')");
                }
                else
                {
                    whereClauses.Add("(Overview is null OR Overview='')");
                }
            }

            if (query.HasOwnerId.HasValue)
            {
                if (query.HasOwnerId.Value)
                {
                    whereClauses.Add("OwnerId not null");
                }
                else
                {
                    whereClauses.Add("OwnerId is null");
                }
            }

            if (!string.IsNullOrWhiteSpace(query.HasNoAudioTrackWithLanguage))
            {
                whereClauses.Add("((select language from MediaStreams where MediaStreams.ItemId=A.Guid and MediaStreams.StreamType='Audio' and MediaStreams.Language=@HasNoAudioTrackWithLanguage limit 1) is null)");
                if (statement != null)
                {
                    statement.TryBind("@HasNoAudioTrackWithLanguage", query.HasNoAudioTrackWithLanguage);
                }
            }

            if (!string.IsNullOrWhiteSpace(query.HasNoInternalSubtitleTrackWithLanguage))
            {
                whereClauses.Add("((select language from MediaStreams where MediaStreams.ItemId=A.Guid and MediaStreams.StreamType='Subtitle' and MediaStreams.IsExternal=0 and MediaStreams.Language=@HasNoInternalSubtitleTrackWithLanguage limit 1) is null)");
                if (statement != null)
                {
                    statement.TryBind("@HasNoInternalSubtitleTrackWithLanguage", query.HasNoInternalSubtitleTrackWithLanguage);
                }
            }

            if (!string.IsNullOrWhiteSpace(query.HasNoExternalSubtitleTrackWithLanguage))
            {
                whereClauses.Add("((select language from MediaStreams where MediaStreams.ItemId=A.Guid and MediaStreams.StreamType='Subtitle' and MediaStreams.IsExternal=1 and MediaStreams.Language=@HasNoExternalSubtitleTrackWithLanguage limit 1) is null)");
                if (statement != null)
                {
                    statement.TryBind("@HasNoExternalSubtitleTrackWithLanguage", query.HasNoExternalSubtitleTrackWithLanguage);
                }
            }

            if (!string.IsNullOrWhiteSpace(query.HasNoSubtitleTrackWithLanguage))
            {
                whereClauses.Add("((select language from MediaStreams where MediaStreams.ItemId=A.Guid and MediaStreams.StreamType='Subtitle' and MediaStreams.Language=@HasNoSubtitleTrackWithLanguage limit 1) is null)");
                if (statement != null)
                {
                    statement.TryBind("@HasNoSubtitleTrackWithLanguage", query.HasNoSubtitleTrackWithLanguage);
                }
            }

            if (query.HasSubtitles.HasValue)
            {
                if (query.HasSubtitles.Value)
                {
                    whereClauses.Add("((select type from MediaStreams where MediaStreams.ItemId=A.Guid and MediaStreams.StreamType='Subtitle' limit 1) not null)");
                }
                else
                {
                    whereClauses.Add("((select type from MediaStreams where MediaStreams.ItemId=A.Guid and MediaStreams.StreamType='Subtitle' limit 1) is null)");
                }
            }

            if (query.HasChapterImages.HasValue)
            {
                if (query.HasChapterImages.Value)
                {
                    whereClauses.Add("((select imagepath from Chapters2 where Chapters2.ItemId=A.Guid and imagepath not null limit 1) not null)");
                }
                else
                {
                    whereClauses.Add("((select imagepath from Chapters2 where Chapters2.ItemId=A.Guid and imagepath not null limit 1) is null)");
                }
            }

            if (query.HasDeadParentId.HasValue && query.HasDeadParentId.Value)
            {
                whereClauses.Add("ParentId NOT NULL AND ParentId NOT IN (select guid from TypedBaseItems)");
            }

            if (query.IsDeadArtist.HasValue && query.IsDeadArtist.Value)
            {
                whereClauses.Add("CleanName not in (Select CleanValue From ItemValues where Type in (0,1))");
            }

            if (query.IsDeadStudio.HasValue && query.IsDeadStudio.Value)
            {
                whereClauses.Add("CleanName not in (Select CleanValue From ItemValues where Type = 3)");
            }

            if (query.IsDeadPerson.HasValue && query.IsDeadPerson.Value)
            {
                whereClauses.Add("Name not in (Select Name From People)");
            }

            if (query.Years.Length == 1)
            {
                whereClauses.Add("ProductionYear=@Years");
                if (statement != null)
                {
                    statement.TryBind("@Years", query.Years[0].ToString(CultureInfo.InvariantCulture));
                }
            }
            else if (query.Years.Length > 1)
            {
                var val = string.Join(',', query.Years);

                whereClauses.Add("ProductionYear in (" + val + ")");
            }

            var isVirtualItem = query.IsVirtualItem ?? query.IsMissing;
            if (isVirtualItem.HasValue)
            {
                whereClauses.Add("IsVirtualItem=@IsVirtualItem");
                if (statement != null)
                {
                    statement.TryBind("@IsVirtualItem", isVirtualItem.Value);
                }
            }

            if (query.IsSpecialSeason.HasValue)
            {
                if (query.IsSpecialSeason.Value)
                {
                    whereClauses.Add("IndexNumber = 0");
                }
                else
                {
                    whereClauses.Add("IndexNumber <> 0");
                }
            }

            if (query.IsUnaired.HasValue)
            {
                if (query.IsUnaired.Value)
                {
                    whereClauses.Add("PremiereDate >= DATETIME('now')");
                }
                else
                {
                    whereClauses.Add("PremiereDate < DATETIME('now')");
                }
            }

            var queryMediaTypes = query.MediaTypes.Where(IsValidMediaType).ToArray();
            if (queryMediaTypes.Length == 1)
            {
                whereClauses.Add("MediaType=@MediaTypes");
                if (statement != null)
                {
                    statement.TryBind("@MediaTypes", queryMediaTypes[0]);
                }
            }
            else if (queryMediaTypes.Length > 1)
            {
                var val = string.Join(',', queryMediaTypes.Select(i => "'" + i + "'"));

                whereClauses.Add("MediaType in (" + val + ")");
            }

            if (query.ItemIds.Length > 0)
            {
                var includeIds = new List<string>();

                var index = 0;
                foreach (var id in query.ItemIds)
                {
                    includeIds.Add("Guid = @IncludeId" + index);
                    if (statement != null)
                    {
                        statement.TryBind("@IncludeId" + index, id);
                    }

                    index++;
                }

                whereClauses.Add("(" + string.Join(" OR ", includeIds) + ")");
            }

            if (query.ExcludeItemIds.Length > 0)
            {
                var excludeIds = new List<string>();

                var index = 0;
                foreach (var id in query.ExcludeItemIds)
                {
                    excludeIds.Add("Guid <> @ExcludeId" + index);
                    if (statement != null)
                    {
                        statement.TryBind("@ExcludeId" + index, id);
                    }

                    index++;
                }

                whereClauses.Add(string.Join(" AND ", excludeIds));
            }

            if (query.ExcludeProviderIds != null && query.ExcludeProviderIds.Count > 0)
            {
                var excludeIds = new List<string>();

                var index = 0;
                foreach (var pair in query.ExcludeProviderIds)
                {
                    if (string.Equals(pair.Key, nameof(MetadataProvider.TmdbCollection), StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var paramName = "@ExcludeProviderId" + index;
                    excludeIds.Add("(ProviderIds is null or ProviderIds not like " + paramName + ")");
                    if (statement != null)
                    {
                        statement.TryBind(paramName, "%" + pair.Key + "=" + pair.Value + "%");
                    }

                    index++;

                    break;
                }

                if (excludeIds.Count > 0)
                {
                    whereClauses.Add(string.Join(" AND ", excludeIds));
                }
            }

            if (query.HasAnyProviderId != null && query.HasAnyProviderId.Count > 0)
            {
                var hasProviderIds = new List<string>();

                var index = 0;
                foreach (var pair in query.HasAnyProviderId)
                {
                    if (string.Equals(pair.Key, nameof(MetadataProvider.TmdbCollection), StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // TODO this seems to be an idea for a better schema where ProviderIds are their own table
                    //      buut this is not implemented
                    // hasProviderIds.Add("(COALESCE((select value from ProviderIds where ItemId=Guid and Name = '" + pair.Key + "'), '') <> " + paramName + ")");

                    // TODO this is a really BAD way to do it since the pair:
                    //      Tmdb, 1234 matches Tmdb=1234 but also Tmdb=1234567
                    //      and maybe even NotTmdb=1234.

                    // this is a placeholder for this specific pair to correlate it in the bigger query
                    var paramName = "@HasAnyProviderId" + index;

                    // this is a search for the placeholder
                    hasProviderIds.Add("ProviderIds like " + paramName);

                    // this replaces the placeholder with a value, here: %key=val%
                    if (statement != null)
                    {
                        statement.TryBind(paramName, "%" + pair.Key + "=" + pair.Value + "%");
                    }

                    index++;

                    break;
                }

                if (hasProviderIds.Count > 0)
                {
                    whereClauses.Add("(" + string.Join(" OR ", hasProviderIds) + ")");
                }
            }

            if (query.HasImdbId.HasValue)
            {
                whereClauses.Add(GetProviderIdClause(query.HasImdbId.Value, "imdb"));
            }

            if (query.HasTmdbId.HasValue)
            {
                whereClauses.Add(GetProviderIdClause(query.HasTmdbId.Value, "tmdb"));
            }

            if (query.HasTvdbId.HasValue)
            {
                whereClauses.Add(GetProviderIdClause(query.HasTvdbId.Value, "tvdb"));
            }

            var queryTopParentIds = query.TopParentIds;

            if (queryTopParentIds.Length > 0)
            {
                var includedItemByNameTypes = GetItemByNameTypesInQuery(query);
                var enableItemsByName = (query.IncludeItemsByName ?? false) && includedItemByNameTypes.Count > 0;

                if (queryTopParentIds.Length == 1)
                {
                    if (enableItemsByName && includedItemByNameTypes.Count == 1)
                    {
                        whereClauses.Add("(TopParentId=@TopParentId or Type=@IncludedItemByNameType)");
                        statement?.TryBind("@IncludedItemByNameType", includedItemByNameTypes[0]);
                    }
                    else if (enableItemsByName && includedItemByNameTypes.Count > 1)
                    {
                        var itemByNameTypeVal = string.Join(',', includedItemByNameTypes.Select(i => "'" + i + "'"));
                        whereClauses.Add("(TopParentId=@TopParentId or Type in (" + itemByNameTypeVal + "))");
                    }
                    else
                    {
                        whereClauses.Add("(TopParentId=@TopParentId)");
                    }

                    statement?.TryBind("@TopParentId", queryTopParentIds[0].ToString("N", CultureInfo.InvariantCulture));
                }
                else if (queryTopParentIds.Length > 1)
                {
                    var val = string.Join(',', queryTopParentIds.Select(i => "'" + i.ToString("N", CultureInfo.InvariantCulture) + "'"));

                    if (enableItemsByName && includedItemByNameTypes.Count == 1)
                    {
                        whereClauses.Add("(Type=@IncludedItemByNameType or TopParentId in (" + val + "))");
                        statement?.TryBind("@IncludedItemByNameType", includedItemByNameTypes[0]);
                    }
                    else if (enableItemsByName && includedItemByNameTypes.Count > 1)
                    {
                        var itemByNameTypeVal = string.Join(',', includedItemByNameTypes.Select(i => "'" + i + "'"));
                        whereClauses.Add("(Type in (" + itemByNameTypeVal + ") or TopParentId in (" + val + "))");
                    }
                    else
                    {
                        whereClauses.Add("TopParentId in (" + val + ")");
                    }
                }
            }

            if (query.AncestorIds.Length == 1)
            {
                whereClauses.Add("Guid in (select itemId from AncestorIds where AncestorId=@AncestorId)");

                if (statement != null)
                {
                    statement.TryBind("@AncestorId", query.AncestorIds[0]);
                }
            }

            if (query.AncestorIds.Length > 1)
            {
                var inClause = string.Join(',', query.AncestorIds.Select(i => "'" + i.ToString("N", CultureInfo.InvariantCulture) + "'"));
                whereClauses.Add(string.Format(CultureInfo.InvariantCulture, "Guid in (select itemId from AncestorIds where AncestorIdText in ({0}))", inClause));
            }

            if (!string.IsNullOrWhiteSpace(query.AncestorWithPresentationUniqueKey))
            {
                var inClause = "select guid from TypedBaseItems where PresentationUniqueKey=@AncestorWithPresentationUniqueKey";
                whereClauses.Add(string.Format(CultureInfo.InvariantCulture, "Guid in (select itemId from AncestorIds where AncestorId in ({0}))", inClause));
                if (statement != null)
                {
                    statement.TryBind("@AncestorWithPresentationUniqueKey", query.AncestorWithPresentationUniqueKey);
                }
            }

            if (!string.IsNullOrWhiteSpace(query.SeriesPresentationUniqueKey))
            {
                whereClauses.Add("SeriesPresentationUniqueKey=@SeriesPresentationUniqueKey");

                if (statement != null)
                {
                    statement.TryBind("@SeriesPresentationUniqueKey", query.SeriesPresentationUniqueKey);
                }
            }

            if (query.BlockUnratedItems.Length == 1)
            {
                whereClauses.Add("(InheritedParentalRatingValue > 0 or UnratedType <> @UnratedType)");
                if (statement != null)
                {
                    statement.TryBind("@UnratedType", query.BlockUnratedItems[0].ToString());
                }
            }

            if (query.BlockUnratedItems.Length > 1)
            {
                var inClause = string.Join(',', query.BlockUnratedItems.Select(i => "'" + i.ToString() + "'"));
                whereClauses.Add(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "(InheritedParentalRatingValue > 0 or UnratedType not in ({0}))",
                        inClause));
            }

            if (query.ExcludeInheritedTags.Length > 0)
            {
                var paramName = "@ExcludeInheritedTags";
                if (statement == null)
                {
                    int index = 0;
                    string excludedTags = string.Join(',', query.ExcludeInheritedTags.Select(_ => paramName + index++));
                    whereClauses.Add("((select CleanValue from ItemValues where ItemId=Guid and Type=6 and cleanvalue in (" + excludedTags + ")) is null)");
                }
                else
                {
                    for (int index = 0; index < query.ExcludeInheritedTags.Length; index++)
                    {
                        statement.TryBind(paramName + index, GetCleanValue(query.ExcludeInheritedTags[index]));
                    }
                }
            }

            if (query.SeriesStatuses.Length > 0)
            {
                var statuses = new List<string>();

                foreach (var seriesStatus in query.SeriesStatuses)
                {
                    statuses.Add("data like  '%" + seriesStatus + "%'");
                }

                whereClauses.Add("(" + string.Join(" OR ", statuses) + ")");
            }

            if (query.BoxSetLibraryFolders.Length > 0)
            {
                var folderIdQueries = new List<string>();

                foreach (var folderId in query.BoxSetLibraryFolders)
                {
                    folderIdQueries.Add("data like '%" + folderId.ToString("N", CultureInfo.InvariantCulture) + "%'");
                }

                whereClauses.Add("(" + string.Join(" OR ", folderIdQueries) + ")");
            }

            if (query.VideoTypes.Length > 0)
            {
                var videoTypes = new List<string>();

                foreach (var videoType in query.VideoTypes)
                {
                    videoTypes.Add("data like '%\"VideoType\":\"" + videoType.ToString() + "\"%'");
                }

                whereClauses.Add("(" + string.Join(" OR ", videoTypes) + ")");
            }

            if (query.Is3D.HasValue)
            {
                if (query.Is3D.Value)
                {
                    whereClauses.Add("data like '%Video3DFormat%'");
                }
                else
                {
                    whereClauses.Add("data not like '%Video3DFormat%'");
                }
            }

            if (query.IsPlaceHolder.HasValue)
            {
                if (query.IsPlaceHolder.Value)
                {
                    whereClauses.Add("data like '%\"IsPlaceHolder\":true%'");
                }
                else
                {
                    whereClauses.Add("(data is null or data not like '%\"IsPlaceHolder\":true%')");
                }
            }

            if (query.HasSpecialFeature.HasValue)
            {
                if (query.HasSpecialFeature.Value)
                {
                    whereClauses.Add("ExtraIds not null");
                }
                else
                {
                    whereClauses.Add("ExtraIds is null");
                }
            }

            if (query.HasTrailer.HasValue)
            {
                if (query.HasTrailer.Value)
                {
                    whereClauses.Add("ExtraIds not null");
                }
                else
                {
                    whereClauses.Add("ExtraIds is null");
                }
            }

            if (query.HasThemeSong.HasValue)
            {
                if (query.HasThemeSong.Value)
                {
                    whereClauses.Add("ExtraIds not null");
                }
                else
                {
                    whereClauses.Add("ExtraIds is null");
                }
            }

            if (query.HasThemeVideo.HasValue)
            {
                if (query.HasThemeVideo.Value)
                {
                    whereClauses.Add("ExtraIds not null");
                }
                else
                {
                    whereClauses.Add("ExtraIds is null");
                }
            }

            return whereClauses;
        }

        /// <summary>
        /// Formats a where clause for the specified provider.
        /// </summary>
        /// <param name="includeResults">Whether or not to include items with this provider's ids.</param>
        /// <param name="provider">Provider name.</param>
        /// <returns>Formatted SQL clause.</returns>
        private string GetProviderIdClause(bool includeResults, string provider)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "ProviderIds {0} like '%{1}=%'",
                includeResults ? string.Empty : "not",
                provider);
        }

        private List<string> GetItemByNameTypesInQuery(InternalItemsQuery query)
        {
            var list = new List<string>();

            if (IsTypeInQuery(BaseItemKind.Person, query))
            {
                list.Add(typeof(Person).FullName);
            }

            if (IsTypeInQuery(BaseItemKind.Genre, query))
            {
                list.Add(typeof(Genre).FullName);
            }

            if (IsTypeInQuery(BaseItemKind.MusicGenre, query))
            {
                list.Add(typeof(MusicGenre).FullName);
            }

            if (IsTypeInQuery(BaseItemKind.MusicArtist, query))
            {
                list.Add(typeof(MusicArtist).FullName);
            }

            if (IsTypeInQuery(BaseItemKind.Studio, query))
            {
                list.Add(typeof(Studio).FullName);
            }

            return list;
        }

        private bool IsTypeInQuery(BaseItemKind type, InternalItemsQuery query)
        {
            if (query.ExcludeItemTypes.Contains(type))
            {
                return false;
            }

            return query.IncludeItemTypes.Length == 0 || query.IncludeItemTypes.Contains(type);
        }

        private string GetCleanValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            return value.RemoveDiacritics().ToLowerInvariant();
        }

        private bool EnableGroupByPresentationUniqueKey(InternalItemsQuery query)
        {
            if (!query.GroupByPresentationUniqueKey)
            {
                return false;
            }

            if (query.GroupBySeriesPresentationUniqueKey)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(query.PresentationUniqueKey))
            {
                return false;
            }

            if (query.User == null)
            {
                return false;
            }

            if (query.IncludeItemTypes.Length == 0)
            {
                return true;
            }

            if (query.IncludeItemTypes.Contains(BaseItemKind.Episode)
                || query.IncludeItemTypes.Contains(BaseItemKind.Video)
                || query.IncludeItemTypes.Contains(BaseItemKind.Movie)
                || query.IncludeItemTypes.Contains(BaseItemKind.MusicVideo)
                || query.IncludeItemTypes.Contains(BaseItemKind.Series)
                || query.IncludeItemTypes.Contains(BaseItemKind.Season))
            {
                return true;
            }

            return false;
        }

        public void UpdateInheritedValues()
        {
            string sql = string.Join(
                ';',
                new string[]
                {
                    "delete from ItemValues where type = 6",

                    "insert into ItemValues (ItemId, Type, Value, CleanValue)  select ItemId, 6, Value, CleanValue from ItemValues where Type=4",

                    @"insert into ItemValues (ItemId, Type, Value, CleanValue) select AncestorIds.itemid, 6, ItemValues.Value, ItemValues.CleanValue
FROM AncestorIds
LEFT JOIN ItemValues ON (AncestorIds.AncestorId = ItemValues.ItemId)
where AncestorIdText not null and ItemValues.Value not null and ItemValues.Type = 4 "
                });

            using (var connection = GetConnection())
            {
                connection.RunInTransaction(
                    db =>
                    {
                        connection.ExecuteAll(sql);
                    },
                    TransactionMode);
            }
        }

        public void DeleteItem(Guid id)
        {
            if (id.Equals(default))
            {
                throw new ArgumentNullException(nameof(id));
            }

            CheckDisposed();

            using (var connection = GetConnection())
            {
                connection.RunInTransaction(
                    db =>
                    {
                        Span<byte> idBlob = stackalloc byte[16];
                        id.TryWriteBytes(idBlob);

                        // Delete people
                        ExecuteWithSingleParam(db, "delete from People where ItemId=@Id", idBlob);

                        // Delete chapters
                        ExecuteWithSingleParam(db, "delete from " + ChaptersTableName + " where ItemId=@Id", idBlob);

                        // Delete media streams
                        ExecuteWithSingleParam(db, "delete from mediastreams where ItemId=@Id", idBlob);

                        // Delete ancestors
                        ExecuteWithSingleParam(db, "delete from AncestorIds where ItemId=@Id", idBlob);

                        // Delete item values
                        ExecuteWithSingleParam(db, "delete from ItemValues where ItemId=@Id", idBlob);

                        // Delete the item
                        ExecuteWithSingleParam(db, "delete from TypedBaseItems where guid=@Id", idBlob);
                    },
                    TransactionMode);
            }
        }

        private void ExecuteWithSingleParam(IDatabaseConnection db, string query, ReadOnlySpan<byte> value)
        {
            using (var statement = PrepareStatement(db, query))
            {
                statement.TryBind("@Id", value);

                statement.MoveNext();
            }
        }

        public List<string> GetPeopleNames(InternalPeopleQuery query)
        {
            ArgumentNullException.ThrowIfNull(query);

            CheckDisposed();

            var commandText = new StringBuilder("select Distinct p.Name from People p");

            var whereClauses = GetPeopleWhereClauses(query, null);

            if (whereClauses.Count != 0)
            {
                commandText.Append(" where ").AppendJoin(" AND ", whereClauses);
            }

            commandText.Append(" order by ListOrder");

            if (query.Limit > 0)
            {
                commandText.Append(" LIMIT ").Append(query.Limit);
            }

            using (var connection = GetConnection(true))
            {
                var list = new List<string>();
                using (var statement = PrepareStatement(connection, commandText.ToString()))
                {
                    // Run this again to bind the params
                    GetPeopleWhereClauses(query, statement);

                    foreach (var row in statement.ExecuteQuery())
                    {
                        list.Add(row.GetString(0));
                    }
                }

                return list;
            }
        }

        public List<PersonInfo> GetPeople(InternalPeopleQuery query)
        {
            ArgumentNullException.ThrowIfNull(query);

            CheckDisposed();

            var commandText = "select ItemId, Name, Role, PersonType, SortOrder from People p";

            var whereClauses = GetPeopleWhereClauses(query, null);

            if (whereClauses.Count != 0)
            {
                commandText += "  where " + string.Join(" AND ", whereClauses);
            }

            commandText += " order by ListOrder";

            if (query.Limit > 0)
            {
                commandText += " LIMIT " + query.Limit;
            }

            using (var connection = GetConnection(true))
            {
                var list = new List<PersonInfo>();

                using (var statement = PrepareStatement(connection, commandText))
                {
                    // Run this again to bind the params
                    GetPeopleWhereClauses(query, statement);

                    foreach (var row in statement.ExecuteQuery())
                    {
                        list.Add(GetPerson(row));
                    }
                }

                return list;
            }
        }

        private List<string> GetPeopleWhereClauses(InternalPeopleQuery query, IStatement statement)
        {
            var whereClauses = new List<string>();

            if (query.User != null && query.IsFavorite.HasValue)
            {
                whereClauses.Add(@"p.Name IN (
SELECT Name FROM TypedBaseItems WHERE UserDataKey IN (
SELECT key FROM UserDatas WHERE isFavorite=@IsFavorite AND userId=@UserId)
AND Type = @InternalPersonType)");
                statement?.TryBind("@IsFavorite", query.IsFavorite.Value);
                statement?.TryBind("@InternalPersonType", typeof(Person).FullName);
                statement?.TryBind("@UserId", query.User.InternalId);
            }

            if (!query.ItemId.Equals(default))
            {
                whereClauses.Add("ItemId=@ItemId");
                statement?.TryBind("@ItemId", query.ItemId);
            }

            if (!query.AppearsInItemId.Equals(default))
            {
                whereClauses.Add("p.Name in (Select Name from People where ItemId=@AppearsInItemId)");
                statement?.TryBind("@AppearsInItemId", query.AppearsInItemId);
            }

            var queryPersonTypes = query.PersonTypes.Where(IsValidPersonType).ToList();

            if (queryPersonTypes.Count == 1)
            {
                whereClauses.Add("PersonType=@PersonType");
                statement?.TryBind("@PersonType", queryPersonTypes[0]);
            }
            else if (queryPersonTypes.Count > 1)
            {
                var val = string.Join(',', queryPersonTypes.Select(i => "'" + i + "'"));

                whereClauses.Add("PersonType in (" + val + ")");
            }

            var queryExcludePersonTypes = query.ExcludePersonTypes.Where(IsValidPersonType).ToList();

            if (queryExcludePersonTypes.Count == 1)
            {
                whereClauses.Add("PersonType<>@PersonType");
                statement?.TryBind("@PersonType", queryExcludePersonTypes[0]);
            }
            else if (queryExcludePersonTypes.Count > 1)
            {
                var val = string.Join(',', queryExcludePersonTypes.Select(i => "'" + i + "'"));

                whereClauses.Add("PersonType not in (" + val + ")");
            }

            if (query.MaxListOrder.HasValue)
            {
                whereClauses.Add("ListOrder<=@MaxListOrder");
                statement?.TryBind("@MaxListOrder", query.MaxListOrder.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.NameContains))
            {
                whereClauses.Add("p.Name like @NameContains");
                statement?.TryBind("@NameContains", "%" + query.NameContains + "%");
            }

            return whereClauses;
        }

        private void UpdateAncestors(Guid itemId, List<Guid> ancestorIds, IDatabaseConnection db, IStatement deleteAncestorsStatement)
        {
            if (itemId.Equals(default))
            {
                throw new ArgumentNullException(nameof(itemId));
            }

            ArgumentNullException.ThrowIfNull(ancestorIds);

            CheckDisposed();

            Span<byte> itemIdBlob = stackalloc byte[16];
            itemId.TryWriteBytes(itemIdBlob);

            // First delete
            deleteAncestorsStatement.Reset();
            deleteAncestorsStatement.TryBind("@ItemId", itemIdBlob);
            deleteAncestorsStatement.MoveNext();

            if (ancestorIds.Count == 0)
            {
                return;
            }

            var insertText = new StringBuilder("insert into AncestorIds (ItemId, AncestorId, AncestorIdText) values ");

            for (var i = 0; i < ancestorIds.Count; i++)
            {
                insertText.AppendFormat(
                    CultureInfo.InvariantCulture,
                    "(@ItemId, @AncestorId{0}, @AncestorIdText{0}),",
                    i.ToString(CultureInfo.InvariantCulture));
            }

            // Remove last ,
            insertText.Length--;

            using (var statement = PrepareStatement(db, insertText.ToString()))
            {
                statement.TryBind("@ItemId", itemIdBlob);

                for (var i = 0; i < ancestorIds.Count; i++)
                {
                    var index = i.ToString(CultureInfo.InvariantCulture);

                    var ancestorId = ancestorIds[i];
                    ancestorId.TryWriteBytes(itemIdBlob);

                    statement.TryBind("@AncestorId" + index, itemIdBlob);
                    statement.TryBind("@AncestorIdText" + index, ancestorId.ToString("N", CultureInfo.InvariantCulture));
                }

                statement.Reset();
                statement.MoveNext();
            }
        }

        public QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetAllArtists(InternalItemsQuery query)
        {
            return GetItemValues(query, new[] { 0, 1 }, typeof(MusicArtist).FullName);
        }

        public QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetArtists(InternalItemsQuery query)
        {
            return GetItemValues(query, new[] { 0 }, typeof(MusicArtist).FullName);
        }

        public QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetAlbumArtists(InternalItemsQuery query)
        {
            return GetItemValues(query, new[] { 1 }, typeof(MusicArtist).FullName);
        }

        public QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetStudios(InternalItemsQuery query)
        {
            return GetItemValues(query, new[] { 3 }, typeof(Studio).FullName);
        }

        public QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetGenres(InternalItemsQuery query)
        {
            return GetItemValues(query, new[] { 2 }, typeof(Genre).FullName);
        }

        public QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetMusicGenres(InternalItemsQuery query)
        {
            return GetItemValues(query, new[] { 2 }, typeof(MusicGenre).FullName);
        }

        public List<string> GetStudioNames()
        {
            return GetItemValueNames(new[] { 3 }, Array.Empty<string>(), Array.Empty<string>());
        }

        public List<string> GetAllArtistNames()
        {
            return GetItemValueNames(new[] { 0, 1 }, Array.Empty<string>(), Array.Empty<string>());
        }

        public List<string> GetMusicGenreNames()
        {
            return GetItemValueNames(
                new[] { 2 },
                new string[]
                {
                    typeof(Audio).FullName,
                    typeof(MusicVideo).FullName,
                    typeof(MusicAlbum).FullName,
                    typeof(MusicArtist).FullName
                },
                Array.Empty<string>());
        }

        public List<string> GetGenreNames()
        {
            return GetItemValueNames(
                new[] { 2 },
                Array.Empty<string>(),
                new string[]
                {
                    typeof(Audio).FullName,
                    typeof(MusicVideo).FullName,
                    typeof(MusicAlbum).FullName,
                    typeof(MusicArtist).FullName
                });
        }

        private List<string> GetItemValueNames(int[] itemValueTypes, IReadOnlyList<string> withItemTypes, IReadOnlyList<string> excludeItemTypes)
        {
            CheckDisposed();

            var now = DateTime.UtcNow;

            var stringBuilder = new StringBuilder("Select Value From ItemValues where Type", 128);
            if (itemValueTypes.Length == 1)
            {
                stringBuilder.Append('=')
                    .Append(itemValueTypes[0]);
            }
            else
            {
                stringBuilder.Append(" in (")
                    .AppendJoin(',', itemValueTypes)
                    .Append(')');
            }

            if (withItemTypes.Count > 0)
            {
                stringBuilder.Append(" AND ItemId In (select guid from typedbaseitems where type in (")
                    .AppendJoinInSingleQuotes(',', withItemTypes)
                    .Append("))");
            }

            if (excludeItemTypes.Count > 0)
            {
                stringBuilder.Append(" AND ItemId not In (select guid from typedbaseitems where type in (")
                    .AppendJoinInSingleQuotes(',', excludeItemTypes)
                    .Append("))");
            }

            stringBuilder.Append(" Group By CleanValue");
            var commandText = stringBuilder.ToString();

            var list = new List<string>();
            using (var connection = GetConnection(true))
            using (var statement = PrepareStatement(connection, commandText))
            {
                foreach (var row in statement.ExecuteQuery())
                {
                    if (row.TryGetString(0, out var result))
                    {
                        list.Add(result);
                    }
                }
            }

            LogQueryTime("GetItemValueNames", commandText, now);
            return list;
        }

        private QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetItemValues(InternalItemsQuery query, int[] itemValueTypes, string returnType)
        {
            ArgumentNullException.ThrowIfNull(query);

            if (!query.Limit.HasValue)
            {
                query.EnableTotalRecordCount = false;
            }

            CheckDisposed();

            var now = DateTime.UtcNow;

            var typeClause = itemValueTypes.Length == 1 ?
                ("Type=" + itemValueTypes[0]) :
                ("Type in (" + string.Join(',', itemValueTypes) + ")");

            InternalItemsQuery typeSubQuery = null;

            string itemCountColumns = null;

            var stringBuilder = new StringBuilder(1024);
            var typesToCount = query.IncludeItemTypes;

            if (typesToCount.Length > 0)
            {
                stringBuilder.Append("(select group_concat(type, '|') from TypedBaseItems B");

                typeSubQuery = new InternalItemsQuery(query.User)
                {
                    ExcludeItemTypes = query.ExcludeItemTypes,
                    IncludeItemTypes = query.IncludeItemTypes,
                    MediaTypes = query.MediaTypes,
                    AncestorIds = query.AncestorIds,
                    ExcludeItemIds = query.ExcludeItemIds,
                    ItemIds = query.ItemIds,
                    TopParentIds = query.TopParentIds,
                    ParentId = query.ParentId,
                    IsPlayed = query.IsPlayed
                };
                var whereClauses = GetWhereClauses(typeSubQuery, null);

                stringBuilder.Append(" where ")
                    .AppendJoin(" AND ", whereClauses)
                    .Append(" AND ")
                    .Append("guid in (select ItemId from ItemValues where ItemValues.CleanValue=A.CleanName AND ")
                    .Append(typeClause)
                    .Append(")) as itemTypes");

                itemCountColumns = stringBuilder.ToString();
                stringBuilder.Clear();
            }

            List<string> columns = _retrieveItemColumns.ToList();
            // Unfortunately we need to add it to columns to ensure the order of the columns in the select
            if (!string.IsNullOrEmpty(itemCountColumns))
            {
                columns.Add(itemCountColumns);
            }

            // do this first before calling GetFinalColumnsToSelect, otherwise ExcludeItemIds will be set by SimilarTo
            var innerQuery = new InternalItemsQuery(query.User)
            {
                ExcludeItemTypes = query.ExcludeItemTypes,
                IncludeItemTypes = query.IncludeItemTypes,
                MediaTypes = query.MediaTypes,
                AncestorIds = query.AncestorIds,
                ItemIds = query.ItemIds,
                TopParentIds = query.TopParentIds,
                ParentId = query.ParentId,
                IsAiring = query.IsAiring,
                IsMovie = query.IsMovie,
                IsSports = query.IsSports,
                IsKids = query.IsKids,
                IsNews = query.IsNews,
                IsSeries = query.IsSeries
            };

            SetFinalColumnsToSelect(query, columns);

            var innerWhereClauses = GetWhereClauses(innerQuery, null);

            stringBuilder.Append(" where Type=@SelectType And CleanName In (Select CleanValue from ItemValues where ")
                .Append(typeClause)
                .Append(" AND ItemId in (select guid from TypedBaseItems");
            if (innerWhereClauses.Count > 0)
            {
                stringBuilder.Append(" where ")
                    .AppendJoin(" AND ", innerWhereClauses);
            }

            stringBuilder.Append("))");

            var outerQuery = new InternalItemsQuery(query.User)
            {
                IsPlayed = query.IsPlayed,
                IsFavorite = query.IsFavorite,
                IsFavoriteOrLiked = query.IsFavoriteOrLiked,
                IsLiked = query.IsLiked,
                IsLocked = query.IsLocked,
                NameLessThan = query.NameLessThan,
                NameStartsWith = query.NameStartsWith,
                NameStartsWithOrGreater = query.NameStartsWithOrGreater,
                Tags = query.Tags,
                OfficialRatings = query.OfficialRatings,
                StudioIds = query.StudioIds,
                GenreIds = query.GenreIds,
                Genres = query.Genres,
                Years = query.Years,
                NameContains = query.NameContains,
                SearchTerm = query.SearchTerm,
                SimilarTo = query.SimilarTo,
                ExcludeItemIds = query.ExcludeItemIds
            };

            var outerWhereClauses = GetWhereClauses(outerQuery, null);
            if (outerWhereClauses.Count != 0)
            {
                stringBuilder.Append(" AND ")
                    .AppendJoin(" AND ", outerWhereClauses);
            }

            var whereText = stringBuilder.ToString();
            stringBuilder.Clear();

            stringBuilder.Append("select ")
                .AppendJoin(',', columns)
                .Append(FromText)
                .Append(GetJoinUserDataText(query))
                .Append(whereText)
                .Append(" group by PresentationUniqueKey");

            if (query.OrderBy.Count != 0
                || query.SimilarTo != null
                || !string.IsNullOrEmpty(query.SearchTerm))
            {
                stringBuilder.Append(GetOrderByText(query));
            }
            else
            {
                stringBuilder.Append(" order by SortName");
            }

            if (query.Limit.HasValue || query.StartIndex.HasValue)
            {
                var offset = query.StartIndex ?? 0;

                if (query.Limit.HasValue || offset > 0)
                {
                    stringBuilder.Append(" LIMIT ")
                        .Append(query.Limit ?? int.MaxValue);
                }

                if (offset > 0)
                {
                    stringBuilder.Append(" OFFSET ")
                        .Append(offset);
                }
            }

            var isReturningZeroItems = query.Limit.HasValue && query.Limit <= 0;

            string commandText = string.Empty;

            if (!isReturningZeroItems)
            {
                commandText = stringBuilder.ToString();
            }

            string countText = string.Empty;
            if (query.EnableTotalRecordCount)
            {
                stringBuilder.Clear();
                var columnsToSelect = new List<string> { "count (distinct PresentationUniqueKey)" };
                SetFinalColumnsToSelect(query, columnsToSelect);
                stringBuilder.Append("select ")
                    .AppendJoin(',', columnsToSelect)
                    .Append(FromText)
                    .Append(GetJoinUserDataText(query))
                    .Append(whereText);

                countText = stringBuilder.ToString();
            }

            var list = new List<(BaseItem, ItemCounts)>();
            var result = new QueryResult<(BaseItem, ItemCounts)>();
            using (var connection = GetConnection(true))
            {
                connection.RunInTransaction(
                    db =>
                    {
                        if (!isReturningZeroItems)
                        {
                            using (var statement = PrepareStatement(db, commandText))
                            {
                                statement.TryBind("@SelectType", returnType);
                                if (EnableJoinUserData(query))
                                {
                                    statement.TryBind("@UserId", query.User.InternalId);
                                }

                                if (typeSubQuery != null)
                                {
                                    GetWhereClauses(typeSubQuery, null);
                                }

                                BindSimilarParams(query, statement);
                                BindSearchParams(query, statement);
                                GetWhereClauses(innerQuery, statement);
                                GetWhereClauses(outerQuery, statement);

                                var hasEpisodeAttributes = HasEpisodeAttributes(query);
                                var hasProgramAttributes = HasProgramAttributes(query);
                                var hasServiceName = HasServiceName(query);
                                var hasStartDate = HasStartDate(query);
                                var hasTrailerTypes = HasTrailerTypes(query);
                                var hasArtistFields = HasArtistFields(query);
                                var hasSeriesFields = HasSeriesFields(query);

                                foreach (var row in statement.ExecuteQuery())
                                {
                                    var item = GetItem(row, query, hasProgramAttributes, hasEpisodeAttributes, hasServiceName, hasStartDate, hasTrailerTypes, hasArtistFields, hasSeriesFields);
                                    if (item != null)
                                    {
                                        var countStartColumn = columns.Count - 1;

                                        list.Add((item, GetItemCounts(row, countStartColumn, typesToCount)));
                                    }
                                }
                            }
                        }

                        if (query.EnableTotalRecordCount)
                        {
                            using (var statement = PrepareStatement(db, countText))
                            {
                                statement.TryBind("@SelectType", returnType);
                                if (EnableJoinUserData(query))
                                {
                                    statement.TryBind("@UserId", query.User.InternalId);
                                }

                                if (typeSubQuery != null)
                                {
                                    GetWhereClauses(typeSubQuery, null);
                                }

                                BindSimilarParams(query, statement);
                                BindSearchParams(query, statement);
                                GetWhereClauses(innerQuery, statement);
                                GetWhereClauses(outerQuery, statement);

                                result.TotalRecordCount = statement.ExecuteQuery().SelectScalarInt().First();
                            }
                        }
                    },
                    ReadTransactionMode);
            }

            LogQueryTime("GetItemValues", commandText, now);

            if (result.TotalRecordCount == 0)
            {
                result.TotalRecordCount = list.Count;
            }

            result.StartIndex = query.StartIndex ?? 0;
            result.Items = list;

            return result;
        }

        private static ItemCounts GetItemCounts(IReadOnlyList<ResultSetValue> reader, int countStartColumn, BaseItemKind[] typesToCount)
        {
            var counts = new ItemCounts();

            if (typesToCount.Length == 0)
            {
                return counts;
            }

            if (!reader.TryGetString(countStartColumn, out var typeString))
            {
                return counts;
            }

            foreach (var typeName in typeString.AsSpan().Split('|'))
            {
                if (typeName.Equals(typeof(Series).FullName, StringComparison.OrdinalIgnoreCase))
                {
                    counts.SeriesCount++;
                }
                else if (typeName.Equals(typeof(Episode).FullName, StringComparison.OrdinalIgnoreCase))
                {
                    counts.EpisodeCount++;
                }
                else if (typeName.Equals(typeof(Movie).FullName, StringComparison.OrdinalIgnoreCase))
                {
                    counts.MovieCount++;
                }
                else if (typeName.Equals(typeof(MusicAlbum).FullName, StringComparison.OrdinalIgnoreCase))
                {
                    counts.AlbumCount++;
                }
                else if (typeName.Equals(typeof(MusicArtist).FullName, StringComparison.OrdinalIgnoreCase))
                {
                    counts.ArtistCount++;
                }
                else if (typeName.Equals(typeof(Audio).FullName, StringComparison.OrdinalIgnoreCase))
                {
                    counts.SongCount++;
                }
                else if (typeName.Equals(typeof(Trailer).FullName, StringComparison.OrdinalIgnoreCase))
                {
                    counts.TrailerCount++;
                }

                counts.ItemCount++;
            }

            return counts;
        }

        private List<(int MagicNumber, string Value)> GetItemValuesToSave(BaseItem item, List<string> inheritedTags)
        {
            var list = new List<(int, string)>();

            if (item is IHasArtist hasArtist)
            {
                list.AddRange(hasArtist.Artists.Select(i => (0, i)));
            }

            if (item is IHasAlbumArtist hasAlbumArtist)
            {
                list.AddRange(hasAlbumArtist.AlbumArtists.Select(i => (1, i)));
            }

            list.AddRange(item.Genres.Select(i => (2, i)));
            list.AddRange(item.Studios.Select(i => (3, i)));
            list.AddRange(item.Tags.Select(i => (4, i)));

            // keywords was 5

            list.AddRange(inheritedTags.Select(i => (6, i)));

            return list;
        }

        private void UpdateItemValues(Guid itemId, List<(int MagicNumber, string Value)> values, IDatabaseConnection db)
        {
            if (itemId.Equals(default))
            {
                throw new ArgumentNullException(nameof(itemId));
            }

            ArgumentNullException.ThrowIfNull(values);

            CheckDisposed();

            var guidBlob = itemId.ToByteArray();

            // First delete
            db.Execute("delete from ItemValues where ItemId=@Id", guidBlob);

            InsertItemValues(guidBlob, values, db);
        }

        private void InsertItemValues(byte[] idBlob, List<(int MagicNumber, string Value)> values, IDatabaseConnection db)
        {
            const int Limit = 100;
            var startIndex = 0;

            const string StartInsertText = "insert into ItemValues (ItemId, Type, Value, CleanValue) values ";
            var insertText = new StringBuilder(StartInsertText);
            while (startIndex < values.Count)
            {
                var endIndex = Math.Min(values.Count, startIndex + Limit);

                for (var i = startIndex; i < endIndex; i++)
                {
                    insertText.AppendFormat(
                        CultureInfo.InvariantCulture,
                        "(@ItemId, @Type{0}, @Value{0}, @CleanValue{0}),",
                        i);
                }

                // Remove last comma
                insertText.Length--;

                using (var statement = PrepareStatement(db, insertText.ToString()))
                {
                    statement.TryBind("@ItemId", idBlob);

                    for (var i = startIndex; i < endIndex; i++)
                    {
                        var index = i.ToString(CultureInfo.InvariantCulture);

                        var currentValueInfo = values[i];

                        var itemValue = currentValueInfo.Value;

                        // Don't save if invalid
                        if (string.IsNullOrWhiteSpace(itemValue))
                        {
                            continue;
                        }

                        statement.TryBind("@Type" + index, currentValueInfo.MagicNumber);
                        statement.TryBind("@Value" + index, itemValue);
                        statement.TryBind("@CleanValue" + index, GetCleanValue(itemValue));
                    }

                    statement.Reset();
                    statement.MoveNext();
                }

                startIndex += Limit;
                insertText.Length = StartInsertText.Length;
            }
        }

        public void UpdatePeople(Guid itemId, List<PersonInfo> people)
        {
            if (itemId.Equals(default))
            {
                throw new ArgumentNullException(nameof(itemId));
            }

            ArgumentNullException.ThrowIfNull(people);

            CheckDisposed();

            using (var connection = GetConnection())
            {
                connection.RunInTransaction(
                    db =>
                    {
                        var itemIdBlob = itemId.ToByteArray();

                        // First delete chapters
                        db.Execute("delete from People where ItemId=@ItemId", itemIdBlob);

                        InsertPeople(itemIdBlob, people, db);
                    },
                    TransactionMode);
            }
        }

        private void InsertPeople(byte[] idBlob, List<PersonInfo> people, IDatabaseConnection db)
        {
            const int Limit = 100;
            var startIndex = 0;
            var listIndex = 0;

            const string StartInsertText = "insert into People (ItemId, Name, Role, PersonType, SortOrder, ListOrder) values ";
            var insertText = new StringBuilder(StartInsertText);
            while (startIndex < people.Count)
            {
                var endIndex = Math.Min(people.Count, startIndex + Limit);
                for (var i = startIndex; i < endIndex; i++)
                {
                    insertText.AppendFormat(
                        CultureInfo.InvariantCulture,
                        "(@ItemId, @Name{0}, @Role{0}, @PersonType{0}, @SortOrder{0}, @ListOrder{0}),",
                        i.ToString(CultureInfo.InvariantCulture));
                }

                // Remove last comma
                insertText.Length--;

                using (var statement = PrepareStatement(db, insertText.ToString()))
                {
                    statement.TryBind("@ItemId", idBlob);

                    for (var i = startIndex; i < endIndex; i++)
                    {
                        var index = i.ToString(CultureInfo.InvariantCulture);

                        var person = people[i];

                        statement.TryBind("@Name" + index, person.Name);
                        statement.TryBind("@Role" + index, person.Role);
                        statement.TryBind("@PersonType" + index, person.Type);
                        statement.TryBind("@SortOrder" + index, person.SortOrder);
                        statement.TryBind("@ListOrder" + index, listIndex);

                        listIndex++;
                    }

                    statement.Reset();
                    statement.MoveNext();
                }

                startIndex += Limit;
                insertText.Length = StartInsertText.Length;
            }
        }

        private PersonInfo GetPerson(IReadOnlyList<ResultSetValue> reader)
        {
            var item = new PersonInfo
            {
                ItemId = reader.GetGuid(0),
                Name = reader.GetString(1)
            };

            if (reader.TryGetString(2, out var role))
            {
                item.Role = role;
            }

            if (reader.TryGetString(3, out var type))
            {
                item.Type = type;
            }

            if (reader.TryGetInt32(4, out var sortOrder))
            {
                item.SortOrder = sortOrder;
            }

            return item;
        }

        public List<MediaStream> GetMediaStreams(MediaStreamQuery query)
        {
            CheckDisposed();

            ArgumentNullException.ThrowIfNull(query);

            var cmdText = _mediaStreamSaveColumnsSelectQuery;

            if (query.Type.HasValue)
            {
                cmdText += " AND StreamType=@StreamType";
            }

            if (query.Index.HasValue)
            {
                cmdText += " AND StreamIndex=@StreamIndex";
            }

            cmdText += " order by StreamIndex ASC";

            using (var connection = GetConnection(true))
            {
                var list = new List<MediaStream>();

                using (var statement = PrepareStatement(connection, cmdText))
                {
                    statement.TryBind("@ItemId", query.ItemId);

                    if (query.Type.HasValue)
                    {
                        statement.TryBind("@StreamType", query.Type.Value.ToString());
                    }

                    if (query.Index.HasValue)
                    {
                        statement.TryBind("@StreamIndex", query.Index.Value);
                    }

                    foreach (var row in statement.ExecuteQuery())
                    {
                        list.Add(GetMediaStream(row));
                    }
                }

                return list;
            }
        }

        public void SaveMediaStreams(Guid id, IReadOnlyList<MediaStream> streams, CancellationToken cancellationToken)
        {
            CheckDisposed();

            if (id.Equals(default))
            {
                throw new ArgumentNullException(nameof(id));
            }

            ArgumentNullException.ThrowIfNull(streams);

            cancellationToken.ThrowIfCancellationRequested();

            using (var connection = GetConnection())
            {
                connection.RunInTransaction(
                db =>
                {
                    var itemIdBlob = id.ToByteArray();

                    // Delete existing mediastreams
                    db.Execute("delete from mediastreams where ItemId=@ItemId", itemIdBlob);

                    InsertMediaStreams(itemIdBlob, streams, db);
                },
                TransactionMode);
            }
        }

        private void InsertMediaStreams(byte[] idBlob, IReadOnlyList<MediaStream> streams, IDatabaseConnection db)
        {
            const int Limit = 10;
            var startIndex = 0;

            var insertText = new StringBuilder(_mediaStreamSaveColumnsInsertQuery);
            while (startIndex < streams.Count)
            {
                var endIndex = Math.Min(streams.Count, startIndex + Limit);

                for (var i = startIndex; i < endIndex; i++)
                {
                    if (i != startIndex)
                    {
                        insertText.Append(',');
                    }

                    var index = i.ToString(CultureInfo.InvariantCulture);
                    insertText.Append("(@ItemId, ");

                    foreach (var column in _mediaStreamSaveColumns.Skip(1))
                    {
                        insertText.Append('@').Append(column).Append(index).Append(',');
                    }

                    insertText.Length -= 1; // Remove the last comma

                    insertText.Append(')');
                }

                using (var statement = PrepareStatement(db, insertText.ToString()))
                {
                    statement.TryBind("@ItemId", idBlob);

                    for (var i = startIndex; i < endIndex; i++)
                    {
                        var index = i.ToString(CultureInfo.InvariantCulture);

                        var stream = streams[i];

                        statement.TryBind("@StreamIndex" + index, stream.Index);
                        statement.TryBind("@StreamType" + index, stream.Type.ToString());
                        statement.TryBind("@Codec" + index, stream.Codec);
                        statement.TryBind("@Language" + index, stream.Language);
                        statement.TryBind("@ChannelLayout" + index, stream.ChannelLayout);
                        statement.TryBind("@Profile" + index, stream.Profile);
                        statement.TryBind("@AspectRatio" + index, stream.AspectRatio);
                        statement.TryBind("@Path" + index, GetPathToSave(stream.Path));

                        statement.TryBind("@IsInterlaced" + index, stream.IsInterlaced);
                        statement.TryBind("@BitRate" + index, stream.BitRate);
                        statement.TryBind("@Channels" + index, stream.Channels);
                        statement.TryBind("@SampleRate" + index, stream.SampleRate);

                        statement.TryBind("@IsDefault" + index, stream.IsDefault);
                        statement.TryBind("@IsForced" + index, stream.IsForced);
                        statement.TryBind("@IsExternal" + index, stream.IsExternal);

                        // Yes these are backwards due to a mistake
                        statement.TryBind("@Width" + index, stream.Height);
                        statement.TryBind("@Height" + index, stream.Width);

                        statement.TryBind("@AverageFrameRate" + index, stream.AverageFrameRate);
                        statement.TryBind("@RealFrameRate" + index, stream.RealFrameRate);
                        statement.TryBind("@Level" + index, stream.Level);

                        statement.TryBind("@PixelFormat" + index, stream.PixelFormat);
                        statement.TryBind("@BitDepth" + index, stream.BitDepth);
                        statement.TryBind("@IsExternal" + index, stream.IsExternal);
                        statement.TryBind("@RefFrames" + index, stream.RefFrames);

                        statement.TryBind("@CodecTag" + index, stream.CodecTag);
                        statement.TryBind("@Comment" + index, stream.Comment);
                        statement.TryBind("@NalLengthSize" + index, stream.NalLengthSize);
                        statement.TryBind("@IsAvc" + index, stream.IsAVC);
                        statement.TryBind("@Title" + index, stream.Title);

                        statement.TryBind("@TimeBase" + index, stream.TimeBase);
                        statement.TryBind("@CodecTimeBase" + index, stream.CodecTimeBase);

                        statement.TryBind("@ColorPrimaries" + index, stream.ColorPrimaries);
                        statement.TryBind("@ColorSpace" + index, stream.ColorSpace);
                        statement.TryBind("@ColorTransfer" + index, stream.ColorTransfer);

                        statement.TryBind("@DvVersionMajor" + index, stream.DvVersionMajor);
                        statement.TryBind("@DvVersionMinor" + index, stream.DvVersionMinor);
                        statement.TryBind("@DvProfile" + index, stream.DvProfile);
                        statement.TryBind("@DvLevel" + index, stream.DvLevel);
                        statement.TryBind("@RpuPresentFlag" + index, stream.RpuPresentFlag);
                        statement.TryBind("@ElPresentFlag" + index, stream.ElPresentFlag);
                        statement.TryBind("@BlPresentFlag" + index, stream.BlPresentFlag);
                        statement.TryBind("@DvBlSignalCompatibilityId" + index, stream.DvBlSignalCompatibilityId);

                        statement.TryBind("@IsHearingImpaired" + index, stream.IsHearingImpaired);
                    }

                    statement.Reset();
                    statement.MoveNext();
                }

                startIndex += Limit;
                insertText.Length = _mediaStreamSaveColumnsInsertQuery.Length;
            }
        }

        /// <summary>
        /// Gets the media stream.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>MediaStream.</returns>
        private MediaStream GetMediaStream(IReadOnlyList<ResultSetValue> reader)
        {
            var item = new MediaStream
            {
                Index = reader[1].ToInt()
            };

            item.Type = Enum.Parse<MediaStreamType>(reader[2].ToString(), true);

            if (reader.TryGetString(3, out var codec))
            {
                item.Codec = codec;
            }

            if (reader.TryGetString(4, out var language))
            {
                item.Language = language;
            }

            if (reader.TryGetString(5, out var channelLayout))
            {
                item.ChannelLayout = channelLayout;
            }

            if (reader.TryGetString(6, out var profile))
            {
                item.Profile = profile;
            }

            if (reader.TryGetString(7, out var aspectRatio))
            {
                item.AspectRatio = aspectRatio;
            }

            if (reader.TryGetString(8, out var path))
            {
                item.Path = RestorePath(path);
            }

            item.IsInterlaced = reader.GetBoolean(9);

            if (reader.TryGetInt32(10, out var bitrate))
            {
                item.BitRate = bitrate;
            }

            if (reader.TryGetInt32(11, out var channels))
            {
                item.Channels = channels;
            }

            if (reader.TryGetInt32(12, out var sampleRate))
            {
                item.SampleRate = sampleRate;
            }

            item.IsDefault = reader.GetBoolean(13);
            item.IsForced = reader.GetBoolean(14);
            item.IsExternal = reader.GetBoolean(15);

            if (reader.TryGetInt32(16, out var width))
            {
                item.Width = width;
            }

            if (reader.TryGetInt32(17, out var height))
            {
                item.Height = height;
            }

            if (reader.TryGetSingle(18, out var averageFrameRate))
            {
                item.AverageFrameRate = averageFrameRate;
            }

            if (reader.TryGetSingle(19, out var realFrameRate))
            {
                item.RealFrameRate = realFrameRate;
            }

            if (reader.TryGetSingle(20, out var level))
            {
                item.Level = level;
            }

            if (reader.TryGetString(21, out var pixelFormat))
            {
                item.PixelFormat = pixelFormat;
            }

            if (reader.TryGetInt32(22, out var bitDepth))
            {
                item.BitDepth = bitDepth;
            }

            if (reader.TryGetBoolean(23, out var isAnamorphic))
            {
                item.IsAnamorphic = isAnamorphic;
            }

            if (reader.TryGetInt32(24, out var refFrames))
            {
                item.RefFrames = refFrames;
            }

            if (reader.TryGetString(25, out var codecTag))
            {
                item.CodecTag = codecTag;
            }

            if (reader.TryGetString(26, out var comment))
            {
                item.Comment = comment;
            }

            if (reader.TryGetString(27, out var nalLengthSize))
            {
                item.NalLengthSize = nalLengthSize;
            }

            if (reader.TryGetBoolean(28, out var isAVC))
            {
                item.IsAVC = isAVC;
            }

            if (reader.TryGetString(29, out var title))
            {
                item.Title = title;
            }

            if (reader.TryGetString(30, out var timeBase))
            {
                item.TimeBase = timeBase;
            }

            if (reader.TryGetString(31, out var codecTimeBase))
            {
                item.CodecTimeBase = codecTimeBase;
            }

            if (reader.TryGetString(32, out var colorPrimaries))
            {
                item.ColorPrimaries = colorPrimaries;
            }

            if (reader.TryGetString(33, out var colorSpace))
            {
                item.ColorSpace = colorSpace;
            }

            if (reader.TryGetString(34, out var colorTransfer))
            {
                item.ColorTransfer = colorTransfer;
            }

            if (reader.TryGetInt32(35, out var dvVersionMajor))
            {
                item.DvVersionMajor = dvVersionMajor;
            }

            if (reader.TryGetInt32(36, out var dvVersionMinor))
            {
                item.DvVersionMinor = dvVersionMinor;
            }

            if (reader.TryGetInt32(37, out var dvProfile))
            {
                item.DvProfile = dvProfile;
            }

            if (reader.TryGetInt32(38, out var dvLevel))
            {
                item.DvLevel = dvLevel;
            }

            if (reader.TryGetInt32(39, out var rpuPresentFlag))
            {
                item.RpuPresentFlag = rpuPresentFlag;
            }

            if (reader.TryGetInt32(40, out var elPresentFlag))
            {
                item.ElPresentFlag = elPresentFlag;
            }

            if (reader.TryGetInt32(41, out var blPresentFlag))
            {
                item.BlPresentFlag = blPresentFlag;
            }

            if (reader.TryGetInt32(42, out var dvBlSignalCompatibilityId))
            {
                item.DvBlSignalCompatibilityId = dvBlSignalCompatibilityId;
            }

            item.IsHearingImpaired = reader.GetBoolean(43);

            if (item.Type == MediaStreamType.Subtitle)
            {
                item.LocalizedUndefined = _localization.GetLocalizedString("Undefined");
                item.LocalizedDefault = _localization.GetLocalizedString("Default");
                item.LocalizedForced = _localization.GetLocalizedString("Forced");
                item.LocalizedExternal = _localization.GetLocalizedString("External");
                item.LocalizedHearingImpaired = _localization.GetLocalizedString("HearingImpaired");
            }

            return item;
        }

        public List<MediaAttachment> GetMediaAttachments(MediaAttachmentQuery query)
        {
            CheckDisposed();

            ArgumentNullException.ThrowIfNull(query);

            var cmdText = _mediaAttachmentSaveColumnsSelectQuery;

            if (query.Index.HasValue)
            {
                cmdText += " AND AttachmentIndex=@AttachmentIndex";
            }

            cmdText += " order by AttachmentIndex ASC";

            var list = new List<MediaAttachment>();
            using (var connection = GetConnection(true))
            using (var statement = PrepareStatement(connection, cmdText))
            {
                statement.TryBind("@ItemId", query.ItemId.ToByteArray());

                if (query.Index.HasValue)
                {
                    statement.TryBind("@AttachmentIndex", query.Index.Value);
                }

                foreach (var row in statement.ExecuteQuery())
                {
                    list.Add(GetMediaAttachment(row));
                }
            }

            return list;
        }

        public void SaveMediaAttachments(
            Guid id,
            IReadOnlyList<MediaAttachment> attachments,
            CancellationToken cancellationToken)
        {
            CheckDisposed();
            if (id.Equals(default))
            {
                throw new ArgumentException("Guid can't be empty.", nameof(id));
            }

            ArgumentNullException.ThrowIfNull(attachments);

            cancellationToken.ThrowIfCancellationRequested();

            using (var connection = GetConnection())
            {
                connection.RunInTransaction(
                db =>
                {
                    var itemIdBlob = id.ToByteArray();

                    db.Execute("delete from mediaattachments where ItemId=@ItemId", itemIdBlob);

                    InsertMediaAttachments(itemIdBlob, attachments, db, cancellationToken);
                },
                TransactionMode);
            }
        }

        private void InsertMediaAttachments(
            byte[] idBlob,
            IReadOnlyList<MediaAttachment> attachments,
            IDatabaseConnection db,
            CancellationToken cancellationToken)
        {
            const int InsertAtOnce = 10;

            var insertText = new StringBuilder(_mediaAttachmentInsertPrefix);
            for (var startIndex = 0; startIndex < attachments.Count; startIndex += InsertAtOnce)
            {
                var endIndex = Math.Min(attachments.Count, startIndex + InsertAtOnce);

                for (var i = startIndex; i < endIndex; i++)
                {
                    var index = i.ToString(CultureInfo.InvariantCulture);
                    insertText.Append("(@ItemId, ");

                    foreach (var column in _mediaAttachmentSaveColumns.Skip(1))
                    {
                        insertText.Append('@')
                            .Append(column)
                            .Append(index)
                            .Append(',');
                    }

                    insertText.Length -= 1;

                    insertText.Append("),");
                }

                insertText.Length--;

                cancellationToken.ThrowIfCancellationRequested();

                using (var statement = PrepareStatement(db, insertText.ToString()))
                {
                    statement.TryBind("@ItemId", idBlob);

                    for (var i = startIndex; i < endIndex; i++)
                    {
                        var index = i.ToString(CultureInfo.InvariantCulture);

                        var attachment = attachments[i];

                        statement.TryBind("@AttachmentIndex" + index, attachment.Index);
                        statement.TryBind("@Codec" + index, attachment.Codec);
                        statement.TryBind("@CodecTag" + index, attachment.CodecTag);
                        statement.TryBind("@Comment" + index, attachment.Comment);
                        statement.TryBind("@Filename" + index, attachment.FileName);
                        statement.TryBind("@MIMEType" + index, attachment.MimeType);
                    }

                    statement.Reset();
                    statement.MoveNext();
                }

                insertText.Length = _mediaAttachmentInsertPrefix.Length;
            }
        }

        /// <summary>
        /// Gets the attachment.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>MediaAttachment.</returns>
        private MediaAttachment GetMediaAttachment(IReadOnlyList<ResultSetValue> reader)
        {
            var item = new MediaAttachment
            {
                Index = reader[1].ToInt()
            };

            if (reader.TryGetString(2, out var codec))
            {
                item.Codec = codec;
            }

            if (reader.TryGetString(3, out var codecTag))
            {
                item.CodecTag = codecTag;
            }

            if (reader.TryGetString(4, out var comment))
            {
                item.Comment = comment;
            }

            if (reader.TryGetString(5, out var fileName))
            {
                item.FileName = fileName;
            }

            if (reader.TryGetString(6, out var mimeType))
            {
                item.MimeType = mimeType;
            }

            return item;
        }
    }
}
