#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
using MediaBrowser.Controller.Extensions;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Querying;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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
            (guid,type,data,Path,StartDate,EndDate,ChannelId,IsMovie,IsSeries,EpisodeTitle,IsRepeat,CommunityRating,CustomRating,IndexNumber,IsLocked,Name,OfficialRating,MediaType,Overview,ParentIndexNumber,PremiereDate,ProductionYear,ParentId,Genres,InheritedParentalRatingValue,SortName,ForcedSortName,RunTimeTicks,Size,DateCreated,DateModified,PreferredMetadataLanguage,PreferredMetadataCountryCode,Width,Height,DateLastRefreshed,DateLastSaved,IsInMixedFolder,LockedFields,Studios,Audio,ExternalServiceId,Tags,IsFolder,UnratedType,TopParentId,TrailerTypes,CriticRating,CleanName,PresentationUniqueKey,OriginalTitle,PrimaryVersionId,DateLastMediaAdded,Album,LUFS,NormalizationGain,IsVirtualItem,SeriesName,UserDataKey,SeasonName,SeasonId,SeriesId,ExternalSeriesId,Tagline,ProviderIds,Images,ProductionLocations,ExtraIds,TotalBitrate,ExtraType,Artists,AlbumArtists,ExternalId,SeriesPresentationUniqueKey,ShowId,OwnerId)
            values (@guid,@type,@data,@Path,@StartDate,@EndDate,@ChannelId,@IsMovie,@IsSeries,@EpisodeTitle,@IsRepeat,@CommunityRating,@CustomRating,@IndexNumber,@IsLocked,@Name,@OfficialRating,@MediaType,@Overview,@ParentIndexNumber,@PremiereDate,@ProductionYear,@ParentId,@Genres,@InheritedParentalRatingValue,@SortName,@ForcedSortName,@RunTimeTicks,@Size,@DateCreated,@DateModified,@PreferredMetadataLanguage,@PreferredMetadataCountryCode,@Width,@Height,@DateLastRefreshed,@DateLastSaved,@IsInMixedFolder,@LockedFields,@Studios,@Audio,@ExternalServiceId,@Tags,@IsFolder,@UnratedType,@TopParentId,@TrailerTypes,@CriticRating,@CleanName,@PresentationUniqueKey,@OriginalTitle,@PrimaryVersionId,@DateLastMediaAdded,@Album,@LUFS,@NormalizationGain,@IsVirtualItem,@SeriesName,@UserDataKey,@SeasonName,@SeasonId,@SeriesId,@ExternalSeriesId,@Tagline,@ProviderIds,@Images,@ProductionLocations,@ExtraIds,@TotalBitrate,@ExtraType,@Artists,@AlbumArtists,@ExternalId,@SeriesPresentationUniqueKey,@ShowId,@OwnerId)";

        private readonly IServerConfigurationManager _config;
        private readonly IServerApplicationHost _appHost;
        private readonly ILocalizationManager _localization;
        // TODO: Remove this dependency. GetImageCacheTag() is the only method used and it can be converted to a static helper method
        private readonly IImageProcessor _imageProcessor;

        private readonly TypeMapper _typeMapper;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteItemRepository"/> class.
        /// </summary>
        /// <param name="config">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        /// <param name="appHost">Instance of the <see cref="IServerApplicationHost"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{SqliteItemRepository}"/> interface.</param>
        /// <param name="localization">Instance of the <see cref="ILocalizationManager"/> interface.</param>
        /// <param name="imageProcessor">Instance of the <see cref="IImageProcessor"/> interface.</param>
        /// <param name="configuration">Instance of the <see cref="IConfiguration"/> interface.</param>
        /// <exception cref="ArgumentNullException">config is null.</exception>
        public SqliteItemRepository(
            IServerConfigurationManager config,
            IServerApplicationHost appHost,
            ILogger<SqliteItemRepository> logger,
            ILocalizationManager localization,
            IImageProcessor imageProcessor,
            IConfiguration configuration)
            : base(logger)
        {
            _config = config;
            _appHost = appHost;
            _localization = localization;
            _imageProcessor = imageProcessor;

            _typeMapper = new TypeMapper();
            _jsonOptions = JsonDefaults.Options;

            DbFilePath = Path.Combine(_config.ApplicationPaths.DataPath, "library.db");

            CacheSize = configuration.GetSqliteCacheSize();
        }

        /// <inheritdoc />
        protected override int? CacheSize { get; }

        /// <inheritdoc />
        protected override TempStoreMode TempStore => TempStoreMode.Memory;


        private bool TypeRequiresDeserialization(Type type)
        {
            if (_config.Configuration.SkipDeserializationForBasicTypes)
            {
                if (type == typeof(Channel)
                    || type == typeof(UserRootFolder))
                {
                    return false;
                }
            }

            return type != typeof(Season)
                && type != typeof(MusicArtist)
                && type != typeof(Person)
                && type != typeof(MusicGenre)
                && type != typeof(Genre)
                && type != typeof(Studio)
                && type != typeof(PlaylistsFolder)
                && type != typeof(PhotoAlbum)
                && type != typeof(Year)
                && type != typeof(Book)
                && type != typeof(LiveTvProgram)
                && type != typeof(AudioBook)
                && type != typeof(MusicAlbum);
        }

        private static bool EnableJoinUserData(InternalItemsQuery query)
        {
            if (query.User is null)
            {
                return false;
            }

            var sortingFields = new HashSet<ItemSortBy>(query.OrderBy.Select(i => i.OrderBy));

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
            if (query.ParentType is not null && _programExcludeParentTypes.Contains(query.ParentType.Value))
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
            if (query.ParentType is not null && _programExcludeParentTypes.Contains(query.ParentType.Value))
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
            if (query.ParentType is not null && _programExcludeParentTypes.Contains(query.ParentType.Value))
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
            if (query.ParentType is not null && _artistExcludeParentTypes.Contains(query.ParentType.Value))
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

            if (query.SimilarTo is not null)
            {
                var item = query.SimilarTo;

                var builder = new StringBuilder();
                builder.Append('(');

                if (item.InheritedParentalRatingValue == 0)
                {
                    builder.Append("((InheritedParentalRatingValue=0) * 10)");
                }
                else
                {
                    builder.Append(
                        @"(SELECT CASE WHEN COALESCE(InheritedParentalRatingValue, 0)=0
                                THEN 0
                                ELSE 10.0 / (1.0 + ABS(InheritedParentalRatingValue - @InheritedParentalRatingValue))
                                END)");
                }

                if (item.ProductionYear.HasValue)
                {
                    builder.Append("+(Select Case When Abs(COALESCE(ProductionYear, 0) - @ItemProductionYear) < 10 Then 10 Else 0 End )");
                    builder.Append("+(Select Case When Abs(COALESCE(ProductionYear, 0) - @ItemProductionYear) < 5 Then 5 Else 0 End )");
                }

                // genres, tags, studios, person, year?
                builder.Append("+ (Select count(1) * 10 from ItemValues where ItemId=Guid and CleanValue in (select CleanValue from ItemValues where ItemId=@SimilarItemId))");
                builder.Append("+ (Select count(1) * 10 from People where ItemId=Guid and Name in (select Name from People where ItemId=@SimilarItemId))");

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

                query.ExcludeItemIds = [.. query.ExcludeItemIds, item.Id, .. item.ExtraIds];
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

        private void BindSearchParams(InternalItemsQuery query, SqliteCommand statement)
        {
            var searchTerm = query.SearchTerm;

            if (string.IsNullOrEmpty(searchTerm))
            {
                return;
            }

            searchTerm = FixUnicodeChars(searchTerm);
            searchTerm = GetCleanValue(searchTerm);

            var commandText = statement.CommandText;
            if (commandText.Contains("@SearchTermStartsWith", StringComparison.OrdinalIgnoreCase))
            {
                statement.TryBind("@SearchTermStartsWith", searchTerm + "%");
            }

            if (commandText.Contains("@SearchTermContains", StringComparison.OrdinalIgnoreCase))
            {
                statement.TryBind("@SearchTermContains", "%" + searchTerm + "%");
            }
        }

        private void BindSimilarParams(InternalItemsQuery query, SqliteCommand statement)
        {
            var item = query.SimilarTo;

            if (item is null)
            {
                return;
            }

            var commandText = statement.CommandText;

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

            if (commandText.Contains("@InheritedParentalRatingValue", StringComparison.OrdinalIgnoreCase))
            {
                statement.TryBind("@InheritedParentalRatingValue", item.InheritedParentalRatingValue);
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

        private string GetOrderByText(InternalItemsQuery query)
        {
            var orderBy = query.OrderBy;
            bool hasSimilar = query.SimilarTo is not null;
            bool hasSearch = !string.IsNullOrEmpty(query.SearchTerm);

            if (hasSimilar || hasSearch)
            {
                List<(ItemSortBy, SortOrder)> prepend = new List<(ItemSortBy, SortOrder)>(4);
                if (hasSearch)
                {
                    prepend.Add((ItemSortBy.SearchScore, SortOrder.Descending));
                    prepend.Add((ItemSortBy.SortName, SortOrder.Ascending));
                }

                if (hasSimilar)
                {
                    prepend.Add((ItemSortBy.SimilarityScore, SortOrder.Descending));
                    prepend.Add((ItemSortBy.Random, SortOrder.Ascending));
                }

                orderBy = query.OrderBy = [.. prepend, .. orderBy];
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

        private string MapOrderByField(ItemSortBy sortBy, InternalItemsQuery query)
        {
            return sortBy switch
            {
                ItemSortBy.AirTime => "SortName", // TODO
                ItemSortBy.Runtime => "RuntimeTicks",
                ItemSortBy.Random => "RANDOM()",
                ItemSortBy.DatePlayed when query.GroupBySeriesPresentationUniqueKey => "MAX(LastPlayedDate)",
                ItemSortBy.DatePlayed => "LastPlayedDate",
                ItemSortBy.PlayCount => "PlayCount",
                ItemSortBy.IsFavoriteOrLiked => "(Select Case When IsFavorite is null Then 0 Else IsFavorite End )",
                ItemSortBy.IsFolder => "IsFolder",
                ItemSortBy.IsPlayed => "played",
                ItemSortBy.IsUnplayed => "played",
                ItemSortBy.DateLastContentAdded => "DateLastMediaAdded",
                ItemSortBy.Artist => "(select CleanValue from ItemValues where ItemId=Guid and Type=0 LIMIT 1)",
                ItemSortBy.AlbumArtist => "(select CleanValue from ItemValues where ItemId=Guid and Type=1 LIMIT 1)",
                ItemSortBy.OfficialRating => "InheritedParentalRatingValue",
                ItemSortBy.Studio => "(select CleanValue from ItemValues where ItemId=Guid and Type=3 LIMIT 1)",
                ItemSortBy.SeriesDatePlayed => "(Select MAX(LastPlayedDate) from TypedBaseItems B" + GetJoinUserDataText(query) + " where Played=1 and B.SeriesPresentationUniqueKey=A.PresentationUniqueKey)",
                ItemSortBy.SeriesSortName => "SeriesName",
                ItemSortBy.AiredEpisodeOrder => "AiredEpisodeOrder",
                ItemSortBy.Album => "Album",
                ItemSortBy.DateCreated => "DateCreated",
                ItemSortBy.PremiereDate => "PremiereDate",
                ItemSortBy.StartDate => "StartDate",
                ItemSortBy.Name => "Name",
                ItemSortBy.CommunityRating => "CommunityRating",
                ItemSortBy.ProductionYear => "ProductionYear",
                ItemSortBy.CriticRating => "CriticRating",
                ItemSortBy.VideoBitRate => "VideoBitRate",
                ItemSortBy.ParentIndexNumber => "ParentIndexNumber",
                ItemSortBy.IndexNumber => "IndexNumber",
                ItemSortBy.SimilarityScore => "SimilarityScore",
                ItemSortBy.SearchScore => "SearchScore",
                _ => "SortName"
            };
        }

        /// <inheritdoc />
        public List<Guid> GetItemIdsList(InternalItemsQuery query)
        {
            ArgumentNullException.ThrowIfNull(query);

            CheckDisposed();

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
            using (new QueryTimeLogger(Logger, commandText))
            using (var connection = GetConnection(true))
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
                    list.Add(row.GetGuid(0));
                }
            }

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

        private bool IsValidPersonType(string value)
        {
            return IsAlphaNumeric(value);
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

        /// <inheritdoc />
        public void UpdateInheritedValues()
        {
            const string Statements = """
delete from ItemValues where type = 6;
insert into ItemValues (ItemId, Type, Value, CleanValue)  select ItemId, 6, Value, CleanValue from ItemValues where Type=4;
insert into ItemValues (ItemId, Type, Value, CleanValue) select AncestorIds.itemid, 6, ItemValues.Value, ItemValues.CleanValue
FROM AncestorIds
LEFT JOIN ItemValues ON (AncestorIds.AncestorId = ItemValues.ItemId)
where AncestorIdText not null and ItemValues.Value not null and ItemValues.Type = 4;
""";
            using var connection = GetConnection();
            using var transaction = connection.BeginTransaction();
            connection.Execute(Statements);
            transaction.Commit();
        }

        /// <inheritdoc />
        public void DeleteItem(Guid id)
        {
            if (id.IsEmpty())
            {
                throw new ArgumentNullException(nameof(id));
            }

            CheckDisposed();

            using var connection = GetConnection();
            using var transaction = connection.BeginTransaction();
            // Delete people
            ExecuteWithSingleParam(connection, "delete from People where ItemId=@Id", id);

            // Delete chapters
            ExecuteWithSingleParam(connection, "delete from " + ChaptersTableName + " where ItemId=@Id", id);

            // Delete media streams
            ExecuteWithSingleParam(connection, "delete from mediastreams where ItemId=@Id", id);

            // Delete ancestors
            ExecuteWithSingleParam(connection, "delete from AncestorIds where ItemId=@Id", id);

            // Delete item values
            ExecuteWithSingleParam(connection, "delete from ItemValues where ItemId=@Id", id);

            // Delete the item
            ExecuteWithSingleParam(connection, "delete from TypedBaseItems where guid=@Id", id);

            transaction.Commit();
        }

        private void ExecuteWithSingleParam(ManagedConnection db, string query, Guid value)
        {
            using (var statement = PrepareStatement(db, query))
            {
                statement.TryBind("@Id", value);

                statement.ExecuteNonQuery();
            }
        }

        /// <inheritdoc />
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

            var list = new List<string>();
            using (var connection = GetConnection(true))
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

        /// <inheritdoc />
        public List<PersonInfo> GetPeople(InternalPeopleQuery query)
        {
            ArgumentNullException.ThrowIfNull(query);

            CheckDisposed();

            StringBuilder commandText = new StringBuilder("select ItemId, Name, Role, PersonType, SortOrder from People p");

            var whereClauses = GetPeopleWhereClauses(query, null);

            if (whereClauses.Count != 0)
            {
                commandText.Append("  where ").AppendJoin(" AND ", whereClauses);
            }

            commandText.Append(" order by ListOrder");

            if (query.Limit > 0)
            {
                commandText.Append(" LIMIT ").Append(query.Limit);
            }

            var list = new List<PersonInfo>();
            using (var connection = GetConnection(true))
            using (var statement = PrepareStatement(connection, commandText.ToString()))
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

        private List<string> GetPeopleWhereClauses(InternalPeopleQuery query, SqliteCommand statement)
        {
            var whereClauses = new List<string>();

            if (query.User is not null && query.IsFavorite.HasValue)
            {
                whereClauses.Add(@"p.Name IN (
SELECT Name FROM TypedBaseItems WHERE UserDataKey IN (
SELECT key FROM UserDatas WHERE isFavorite=@IsFavorite AND userId=@UserId)
AND Type = @InternalPersonType)");
                statement?.TryBind("@IsFavorite", query.IsFavorite.Value);
                statement?.TryBind("@InternalPersonType", typeof(Person).FullName);
                statement?.TryBind("@UserId", query.User.InternalId);
            }

            if (!query.ItemId.IsEmpty())
            {
                whereClauses.Add("ItemId=@ItemId");
                statement?.TryBind("@ItemId", query.ItemId);
            }

            if (!query.AppearsInItemId.IsEmpty())
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

        private void UpdateAncestors(Guid itemId, List<Guid> ancestorIds, ManagedConnection db, SqliteCommand deleteAncestorsStatement)
        {
            if (itemId.IsEmpty())
            {
                throw new ArgumentNullException(nameof(itemId));
            }

            ArgumentNullException.ThrowIfNull(ancestorIds);

            CheckDisposed();

            // First delete
            deleteAncestorsStatement.TryBind("@ItemId", itemId);
            deleteAncestorsStatement.ExecuteNonQuery();

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

            // Remove trailing comma
            insertText.Length--;

            using (var statement = PrepareStatement(db, insertText.ToString()))
            {
                statement.TryBind("@ItemId", itemId);

                for (var i = 0; i < ancestorIds.Count; i++)
                {
                    var index = i.ToString(CultureInfo.InvariantCulture);

                    var ancestorId = ancestorIds[i];

                    statement.TryBind("@AncestorId" + index, ancestorId);
                    statement.TryBind("@AncestorIdText" + index, ancestorId.ToString("N", CultureInfo.InvariantCulture));
                }

                statement.ExecuteNonQuery();
            }
        }

        /// <inheritdoc />
        public QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetAllArtists(InternalItemsQuery query)
        {
            return GetItemValues(query, new[] { 0, 1 }, typeof(MusicArtist).FullName);
        }

        /// <inheritdoc />
        public QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetArtists(InternalItemsQuery query)
        {
            return GetItemValues(query, new[] { 0 }, typeof(MusicArtist).FullName);
        }

        /// <inheritdoc />
        public QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetAlbumArtists(InternalItemsQuery query)
        {
            return GetItemValues(query, new[] { 1 }, typeof(MusicArtist).FullName);
        }

        /// <inheritdoc />
        public QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetStudios(InternalItemsQuery query)
        {
            return GetItemValues(query, new[] { 3 }, typeof(Studio).FullName);
        }

        /// <inheritdoc />
        public QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetGenres(InternalItemsQuery query)
        {
            return GetItemValues(query, new[] { 2 }, typeof(Genre).FullName);
        }

        /// <inheritdoc />
        public QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetMusicGenres(InternalItemsQuery query)
        {
            return GetItemValues(query, new[] { 2 }, typeof(MusicGenre).FullName);
        }

        /// <inheritdoc />
        public List<string> GetStudioNames()
        {
            return GetItemValueNames(new[] { 3 }, Array.Empty<string>(), Array.Empty<string>());
        }

        /// <inheritdoc />
        public List<string> GetAllArtistNames()
        {
            return GetItemValueNames(new[] { 0, 1 }, Array.Empty<string>(), Array.Empty<string>());
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
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
            using (new QueryTimeLogger(Logger, commandText))
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
                || query.SimilarTo is not null
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
            using (new QueryTimeLogger(Logger, commandText))
            using (var connection = GetConnection(true))
            using (var transaction = connection.BeginTransaction())
            {
                if (!isReturningZeroItems)
                {
                    using (var statement = PrepareStatement(connection, commandText))
                    {
                        statement.TryBind("@SelectType", returnType);
                        if (EnableJoinUserData(query))
                        {
                            statement.TryBind("@UserId", query.User.InternalId);
                        }

                        if (typeSubQuery is not null)
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
                            var item = GetItem(row, query, hasProgramAttributes, hasEpisodeAttributes, hasServiceName, hasStartDate, hasTrailerTypes, hasArtistFields, hasSeriesFields, false);
                            if (item is not null)
                            {
                                var countStartColumn = columns.Count - 1;

                                list.Add((item, GetItemCounts(row, countStartColumn, typesToCount)));
                            }
                        }
                    }
                }

                if (query.EnableTotalRecordCount)
                {
                    using (var statement = PrepareStatement(connection, countText))
                    {
                        statement.TryBind("@SelectType", returnType);
                        if (EnableJoinUserData(query))
                        {
                            statement.TryBind("@UserId", query.User.InternalId);
                        }

                        if (typeSubQuery is not null)
                        {
                            GetWhereClauses(typeSubQuery, null);
                        }

                        BindSimilarParams(query, statement);
                        BindSearchParams(query, statement);
                        GetWhereClauses(innerQuery, statement);
                        GetWhereClauses(outerQuery, statement);

                        result.TotalRecordCount = statement.SelectScalarInt();
                    }
                }

                transaction.Commit();
            }

            if (result.TotalRecordCount == 0)
            {
                result.TotalRecordCount = list.Count;
            }

            result.StartIndex = query.StartIndex ?? 0;
            result.Items = list;

            return result;
        }

        private static ItemCounts GetItemCounts(SqliteDataReader reader, int countStartColumn, BaseItemKind[] typesToCount)
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

            // Remove all invalid values.
            list.RemoveAll(i => string.IsNullOrWhiteSpace(i.Item2));

            return list;
        }

        private void UpdateItemValues(Guid itemId, List<(int MagicNumber, string Value)> values, ManagedConnection db)
        {
            if (itemId.IsEmpty())
            {
                throw new ArgumentNullException(nameof(itemId));
            }

            ArgumentNullException.ThrowIfNull(values);

            CheckDisposed();

            // First delete
            using var command = db.PrepareStatement("delete from ItemValues where ItemId=@Id");
            command.TryBind("@Id", itemId);
            command.ExecuteNonQuery();

            InsertItemValues(itemId, values, db);
        }

        private void InsertItemValues(Guid id, List<(int MagicNumber, string Value)> values, ManagedConnection db)
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

                // Remove trailing comma
                insertText.Length--;

                using (var statement = PrepareStatement(db, insertText.ToString()))
                {
                    statement.TryBind("@ItemId", id);

                    for (var i = startIndex; i < endIndex; i++)
                    {
                        var index = i.ToString(CultureInfo.InvariantCulture);

                        var currentValueInfo = values[i];

                        var itemValue = currentValueInfo.Value;

                        statement.TryBind("@Type" + index, currentValueInfo.MagicNumber);
                        statement.TryBind("@Value" + index, itemValue);
                        statement.TryBind("@CleanValue" + index, GetCleanValue(itemValue));
                    }

                    statement.ExecuteNonQuery();
                }

                startIndex += Limit;
                insertText.Length = StartInsertText.Length;
            }
        }

        /// <inheritdoc />
        public void UpdatePeople(Guid itemId, List<PersonInfo> people)
        {
            if (itemId.IsEmpty())
            {
                throw new ArgumentNullException(nameof(itemId));
            }

            CheckDisposed();

            using var connection = GetConnection();
            using var transaction = connection.BeginTransaction();
            // Delete all existing people first
            using var command = connection.CreateCommand();
            command.CommandText = "delete from People where ItemId=@ItemId";
            command.TryBind("@ItemId", itemId);
            command.ExecuteNonQuery();

            if (people is not null)
            {
                InsertPeople(itemId, people, connection);
            }

            transaction.Commit();
        }

        private void InsertPeople(Guid id, List<PersonInfo> people, ManagedConnection db)
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

                // Remove trailing comma
                insertText.Length--;

                using (var statement = PrepareStatement(db, insertText.ToString()))
                {
                    statement.TryBind("@ItemId", id);

                    for (var i = startIndex; i < endIndex; i++)
                    {
                        var index = i.ToString(CultureInfo.InvariantCulture);

                        var person = people[i];

                        statement.TryBind("@Name" + index, person.Name);
                        statement.TryBind("@Role" + index, person.Role);
                        statement.TryBind("@PersonType" + index, person.Type.ToString());
                        statement.TryBind("@SortOrder" + index, person.SortOrder);
                        statement.TryBind("@ListOrder" + index, listIndex);

                        listIndex++;
                    }

                    statement.ExecuteNonQuery();
                }

                startIndex += Limit;
                insertText.Length = StartInsertText.Length;
            }
        }

        private PersonInfo GetPerson(SqliteDataReader reader)
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

            if (reader.TryGetString(3, out var type)
                && Enum.TryParse(type, true, out PersonKind personKind))
            {
                item.Type = personKind;
            }

            if (reader.TryGetInt32(4, out var sortOrder))
            {
                item.SortOrder = sortOrder;
            }

            return item;
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public void SaveMediaStreams(Guid id, IReadOnlyList<MediaStream> streams, CancellationToken cancellationToken)
        {
            CheckDisposed();

            if (id.IsEmpty())
            {
                throw new ArgumentNullException(nameof(id));
            }

            ArgumentNullException.ThrowIfNull(streams);

            cancellationToken.ThrowIfCancellationRequested();

            using var connection = GetConnection();
            using var transaction = connection.BeginTransaction();
            // Delete existing mediastreams
            using var command = connection.PrepareStatement("delete from mediastreams where ItemId=@ItemId");
            command.TryBind("@ItemId", id);
            command.ExecuteNonQuery();

            InsertMediaStreams(id, streams, connection);

            transaction.Commit();
        }

        private void InsertMediaStreams(Guid id, IReadOnlyList<MediaStream> streams, ManagedConnection db)
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
                    statement.TryBind("@ItemId", id);

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
                        statement.TryBind("@IsAnamorphic" + index, stream.IsAnamorphic);
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

                        statement.TryBind("@Rotation" + index, stream.Rotation);
                    }

                    statement.ExecuteNonQuery();
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
        private MediaStream GetMediaStream(SqliteDataReader reader)
        {
            var item = new MediaStream
            {
                Index = reader.GetInt32(1),
                Type = Enum.Parse<MediaStreamType>(reader.GetString(2), true)
            };

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

            item.IsHearingImpaired = reader.TryGetBoolean(43, out var result) && result;

            if (reader.TryGetInt32(44, out var rotation))
            {
                item.Rotation = rotation;
            }

            if (item.Type is MediaStreamType.Audio or MediaStreamType.Subtitle)
            {
                item.LocalizedDefault = _localization.GetLocalizedString("Default");
                item.LocalizedExternal = _localization.GetLocalizedString("External");

                if (item.Type is MediaStreamType.Subtitle)
                {
                    item.LocalizedUndefined = _localization.GetLocalizedString("Undefined");
                    item.LocalizedForced = _localization.GetLocalizedString("Forced");
                    item.LocalizedHearingImpaired = _localization.GetLocalizedString("HearingImpaired");
                }
            }

            return item;
        }

        /// <inheritdoc />
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
                statement.TryBind("@ItemId", query.ItemId);

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

        /// <inheritdoc />
        public void SaveMediaAttachments(
            Guid id,
            IReadOnlyList<MediaAttachment> attachments,
            CancellationToken cancellationToken)
        {
            CheckDisposed();
            if (id.IsEmpty())
            {
                throw new ArgumentException("Guid can't be empty.", nameof(id));
            }

            ArgumentNullException.ThrowIfNull(attachments);

            cancellationToken.ThrowIfCancellationRequested();

            using (var connection = GetConnection())
            using (var transaction = connection.BeginTransaction())
            using (var command = connection.PrepareStatement("delete from mediaattachments where ItemId=@ItemId"))
            {
                command.TryBind("@ItemId", id);
                command.ExecuteNonQuery();

                InsertMediaAttachments(id, attachments, connection, cancellationToken);

                transaction.Commit();
            }
        }

        private void InsertMediaAttachments(
            Guid id,
            IReadOnlyList<MediaAttachment> attachments,
            ManagedConnection db,
            CancellationToken cancellationToken)
        {
            const int InsertAtOnce = 10;

            var insertText = new StringBuilder(_mediaAttachmentInsertPrefix);
            for (var startIndex = 0; startIndex < attachments.Count; startIndex += InsertAtOnce)
            {
                var endIndex = Math.Min(attachments.Count, startIndex + InsertAtOnce);

                for (var i = startIndex; i < endIndex; i++)
                {
                    insertText.Append("(@ItemId, ");

                    foreach (var column in _mediaAttachmentSaveColumns.Skip(1))
                    {
                        insertText.Append('@')
                            .Append(column)
                            .Append(i)
                            .Append(',');
                    }

                    insertText.Length -= 1;

                    insertText.Append("),");
                }

                insertText.Length--;

                cancellationToken.ThrowIfCancellationRequested();

                using (var statement = PrepareStatement(db, insertText.ToString()))
                {
                    statement.TryBind("@ItemId", id);

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

                    statement.ExecuteNonQuery();
                }

                insertText.Length = _mediaAttachmentInsertPrefix.Length;
            }
        }

        /// <summary>
        /// Gets the attachment.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>MediaAttachment.</returns>
        private MediaAttachment GetMediaAttachment(SqliteDataReader reader)
        {
            var item = new MediaAttachment
            {
                Index = reader.GetInt32(1)
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

        private static string BuildMediaAttachmentInsertPrefix()
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
            return queryPrefixText.ToString();
        }

#nullable enable

        private readonly struct QueryTimeLogger : IDisposable
        {
            private readonly ILogger _logger;
            private readonly string _commandText;
            private readonly string _methodName;
            private readonly long _startTimestamp;

            public QueryTimeLogger(ILogger logger, string commandText, [CallerMemberName] string methodName = "")
            {
                _logger = logger;
                _commandText = commandText;
                _methodName = methodName;
                _startTimestamp = logger.IsEnabled(LogLevel.Debug) ? Stopwatch.GetTimestamp() : -1;
            }

            public void Dispose()
            {
                if (_startTimestamp == -1)
                {
                    return;
                }

                var elapsedMs = Stopwatch.GetElapsedTime(_startTimestamp).TotalMilliseconds;

#if DEBUG
                const int SlowThreshold = 100;
#else
                const int SlowThreshold = 10;
#endif

                if (elapsedMs >= SlowThreshold)
                {
                    _logger.LogDebug(
                        "{Method} query time (slow): {ElapsedMs}ms. Query: {Query}",
                        _methodName,
                        elapsedMs,
                        _commandText);
                }
            }
        }
    }
}
