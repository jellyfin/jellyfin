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

        private string GetJoinUserDataText(InternalItemsQuery query)
        {
            if (!EnableJoinUserData(query))
            {
                return string.Empty;
            }

            return " left join UserDatas on UserDataKey=UserDatas.Key And (UserId=@UserId)";
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
