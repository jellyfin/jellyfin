#pragma warning disable RS0030 // Do not use banned APIs

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Emby.Server.Implementations.Data;
using Jellyfin.Data.Entities;
using Jellyfin.Extensions;
using Jellyfin.Server.Implementations;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Chapter = Jellyfin.Data.Entities.Chapter;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// The migration routine for migrating the userdata database to EF Core.
/// </summary>
public class MigrateLibraryDb : IMigrationRoutine
{
    private const string DbFilename = "library.db";

    private readonly ILogger<MigrateLibraryDb> _logger;
    private readonly IServerApplicationPaths _paths;
    private readonly IDbContextFactory<JellyfinDbContext> _provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrateLibraryDb"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="provider">The database provider.</param>
    /// <param name="paths">The server application paths.</param>
    public MigrateLibraryDb(
        ILogger<MigrateLibraryDb> logger,
        IDbContextFactory<JellyfinDbContext> provider,
        IServerApplicationPaths paths)
    {
        _logger = logger;
        _provider = provider;
        _paths = paths;
    }

    /// <inheritdoc/>
    public Guid Id => Guid.Parse("36445464-849f-429f-9ad0-bb130efa0664");

    /// <inheritdoc/>
    public string Name => "MigrateLibraryDbData";

    /// <inheritdoc/>
    public bool PerformOnNewInstall => false; // TODO Change back after testing

    /// <inheritdoc/>
    public void Perform()
    {
        _logger.LogInformation("Migrating the userdata from library.db may take a while, do not stop Jellyfin.");

        var dataPath = _paths.DataPath;
        var libraryDbPath = Path.Combine(dataPath, DbFilename);
        using var connection = new SqliteConnection($"Filename={libraryDbPath}");
        var migrationTotalTime = TimeSpan.Zero;

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        connection.Open();
        using var dbContext = _provider.CreateDbContext();

        migrationTotalTime += stopwatch.Elapsed;
        _logger.LogInformation("Saving UserData entries took {0}.", stopwatch.Elapsed);
        stopwatch.Restart();

        _logger.LogInformation("Start moving TypedBaseItem.");
        const string typedBaseItemsQuery = """
         SELECT guid, type, data, StartDate, EndDate, ChannelId, IsMovie,
         IsSeries, EpisodeTitle, IsRepeat, CommunityRating, CustomRating, IndexNumber, IsLocked, PreferredMetadataLanguage,
         PreferredMetadataCountryCode, Width, Height, DateLastRefreshed, Name, Path, PremiereDate, Overview, ParentIndexNumber,
         ProductionYear, OfficialRating, ForcedSortName, RunTimeTicks, Size, DateCreated, DateModified, Genres, ParentId, TopParentId,
         Audio, ExternalServiceId, IsInMixedFolder, DateLastSaved, LockedFields, Studios, Tags, TrailerTypes, OriginalTitle, PrimaryVersionId,
         DateLastMediaAdded, Album, LUFS, NormalizationGain, CriticRating, IsVirtualItem, SeriesName, UserDataKey, SeasonName, SeasonId, SeriesId,
         PresentationUniqueKey, InheritedParentalRatingValue, ExternalSeriesId, Tagline, ProviderIds, Images, ProductionLocations, ExtraIds, TotalBitrate,
         ExtraType, Artists, AlbumArtists, ExternalId, SeriesPresentationUniqueKey, ShowId, OwnerId, MediaType FROM TypedBaseItems
         """;
        dbContext.BaseItems.ExecuteDelete();

        var legacyBaseItemWithUserKeys = new Dictionary<string, BaseItemEntity>();
        foreach (SqliteDataReader dto in connection.Query(typedBaseItemsQuery))
        {
            var baseItem = GetItem(dto);
            dbContext.BaseItems.Add(baseItem.BaseItem);
            foreach (var dataKey in baseItem.LegacyUserDataKey)
            {
                legacyBaseItemWithUserKeys[dataKey] = baseItem.BaseItem;
            }
        }

        _logger.LogInformation("Try saving {0} BaseItem entries.", dbContext.BaseItems.Local.Count);
        dbContext.SaveChanges();
        migrationTotalTime += stopwatch.Elapsed;
        _logger.LogInformation("Saving BaseItems entries took {0}.", stopwatch.Elapsed);
        stopwatch.Restart();

        _logger.LogInformation("Start moving ItemValues.");
        // do not migrate inherited types as they are now properly mapped in search and lookup.
        const string itemValueQuery =
        """
        SELECT ItemId, Type, Value, CleanValue FROM ItemValues
                    WHERE Type <> 6 AND EXISTS(SELECT 1 FROM TypedBaseItems WHERE TypedBaseItems.guid = ItemValues.ItemId)
        """;
        dbContext.ItemValues.ExecuteDelete();

        // EFCores local lookup sucks. We cannot use context.ItemValues.Local here because its just super slow.
        var localItems = new Dictionary<(int Type, string CleanValue), (ItemValue ItemValue, List<Guid> ItemIds)>();

        foreach (SqliteDataReader dto in connection.Query(itemValueQuery))
        {
            var itemId = dto.GetGuid(0);
            var entity = GetItemValue(dto);
            var key = ((int)entity.Type, entity.CleanValue);
            if (!localItems.TryGetValue(key, out var existing))
            {
                localItems[key] = existing = (entity, []);
            }

            existing.ItemIds.Add(itemId);
        }

        foreach (var item in localItems)
        {
            dbContext.ItemValues.Add(item.Value.ItemValue);
            dbContext.ItemValuesMap.AddRange(item.Value.ItemIds.Distinct().Select(f => new ItemValueMap()
            {
                Item = null!,
                ItemValue = null!,
                ItemId = f,
                ItemValueId = item.Value.ItemValue.ItemValueId
            }));
        }

        _logger.LogInformation("Try saving {0} ItemValues entries.", dbContext.ItemValues.Local.Count);
        dbContext.SaveChanges();
        migrationTotalTime += stopwatch.Elapsed;
        _logger.LogInformation("Saving People ItemValues took {0}.", stopwatch.Elapsed);
        stopwatch.Restart();

        _logger.LogInformation("Start moving UserData.");
        var queryResult = connection.Query("""
        SELECT key, userId, rating, played, playCount, isFavorite, playbackPositionTicks, lastPlayedDate, AudioStreamIndex, SubtitleStreamIndex FROM UserDatas

        WHERE EXISTS(SELECT 1 FROM TypedBaseItems WHERE TypedBaseItems.UserDataKey = UserDatas.key)
        """);

        dbContext.UserData.ExecuteDelete();

        var users = dbContext.Users.AsNoTracking().ToImmutableArray();
        var oldUserdata = new Dictionary<string, UserData>();

        foreach (var entity in queryResult)
        {
            var userData = GetUserData(users, entity);
            if (userData is null)
            {
                _logger.LogError("Was not able to migrate user data with key {0}", entity.GetString(0));
                continue;
            }

            if (!legacyBaseItemWithUserKeys.TryGetValue(userData.CustomDataKey!, out var refItem))
            {
                _logger.LogError("Was not able to migrate user data with key {0} because it does not reference a valid BaseItem.", entity.GetString(0));
                continue;
            }

            userData.ItemId = refItem.Id;
            dbContext.UserData.Add(userData);
        }

        _logger.LogInformation("Try saving {0} UserData entries.", dbContext.UserData.Local.Count);
        dbContext.SaveChanges();

        _logger.LogInformation("Start moving MediaStreamInfos.");
        const string mediaStreamQuery = """
        SELECT ItemId, StreamIndex, StreamType, Codec, Language, ChannelLayout, Profile, AspectRatio, Path,
        IsInterlaced, BitRate, Channels, SampleRate, IsDefault, IsForced, IsExternal, Height, Width,
        AverageFrameRate, RealFrameRate, Level, PixelFormat, BitDepth, IsAnamorphic, RefFrames, CodecTag,
        Comment, NalLengthSize, IsAvc, Title, TimeBase, CodecTimeBase, ColorPrimaries, ColorSpace, ColorTransfer,
        DvVersionMajor, DvVersionMinor, DvProfile, DvLevel, RpuPresentFlag, ElPresentFlag, BlPresentFlag, DvBlSignalCompatibilityId, IsHearingImpaired
        FROM MediaStreams
        WHERE EXISTS(SELECT 1 FROM TypedBaseItems WHERE TypedBaseItems.guid = MediaStreams.ItemId)
        """;
        dbContext.MediaStreamInfos.ExecuteDelete();

        foreach (SqliteDataReader dto in connection.Query(mediaStreamQuery))
        {
            dbContext.MediaStreamInfos.Add(GetMediaStream(dto));
        }

        _logger.LogInformation("Try saving {0} MediaStreamInfos entries.", dbContext.MediaStreamInfos.Local.Count);
        dbContext.SaveChanges();

        migrationTotalTime += stopwatch.Elapsed;
        _logger.LogInformation("Saving MediaStreamInfos entries took {0}.", stopwatch.Elapsed);
        stopwatch.Restart();

        _logger.LogInformation("Start moving People.");
        const string personsQuery = """
        SELECT ItemId, Name, Role, PersonType, SortOrder FROM People
        WHERE EXISTS(SELECT 1 FROM TypedBaseItems WHERE TypedBaseItems.guid = People.ItemId)
        """;
        dbContext.Peoples.ExecuteDelete();
        dbContext.PeopleBaseItemMap.ExecuteDelete();

        var peopleCache = new Dictionary<string, (People Person, List<PeopleBaseItemMap> Items)>();

        foreach (SqliteDataReader reader in connection.Query(personsQuery))
        {
            var itemId = reader.GetGuid(0);
            if (!dbContext.BaseItems.Any(f => f.Id == itemId))
            {
                _logger.LogError("Dont save person {0} because its not in use by any BaseItem", reader.GetString(1));
                continue;
            }

            var entity = GetPerson(reader);
            if (!peopleCache.TryGetValue(entity.Name, out var personCache))
            {
                peopleCache[entity.Name] = personCache = (entity, []);
            }

            if (reader.TryGetString(2, out var role))
            {
            }

            if (reader.TryGetInt32(4, out var sortOrder))
            {
            }

            personCache.Items.Add(new PeopleBaseItemMap()
            {
                Item = null!,
                ItemId = itemId,
                People = null!,
                PeopleId = personCache.Person.Id,
                ListOrder = sortOrder,
                SortOrder = sortOrder,
                Role = role
            });
        }

        foreach (var item in peopleCache)
        {
            dbContext.Peoples.Add(item.Value.Person);
            dbContext.PeopleBaseItemMap.AddRange(item.Value.Items.DistinctBy(e => (e.ItemId, e.PeopleId)));
        }

        _logger.LogInformation("Try saving {0} People entries.", dbContext.MediaStreamInfos.Local.Count);
        dbContext.SaveChanges();
        migrationTotalTime += stopwatch.Elapsed;
        _logger.LogInformation("Saving People entries took {0}.", stopwatch.Elapsed);
        stopwatch.Restart();

        _logger.LogInformation("Start moving Chapters.");
        const string chapterQuery = """
        SELECT ItemId,StartPositionTicks,Name,ImagePath,ImageDateModified,ChapterIndex from Chapters2
        WHERE EXISTS(SELECT 1 FROM TypedBaseItems WHERE TypedBaseItems.guid = Chapters2.ItemId)
        """;
        dbContext.Chapters.ExecuteDelete();

        foreach (SqliteDataReader dto in connection.Query(chapterQuery))
        {
            var chapter = GetChapter(dto);
            dbContext.Chapters.Add(chapter);
        }

        _logger.LogInformation("Try saving {0} Chapters entries.", dbContext.Chapters.Local.Count);
        dbContext.SaveChanges();
        migrationTotalTime += stopwatch.Elapsed;
        _logger.LogInformation("Saving Chapters took {0}.", stopwatch.Elapsed);
        stopwatch.Restart();

        _logger.LogInformation("Start moving AncestorIds.");
        const string ancestorIdsQuery = """
        SELECT ItemId, AncestorId, AncestorIdText FROM AncestorIds
        WHERE
        EXISTS(SELECT 1 FROM TypedBaseItems WHERE TypedBaseItems.guid = AncestorIds.ItemId)
        AND
        EXISTS(SELECT 1 FROM TypedBaseItems WHERE TypedBaseItems.guid = AncestorIds.AncestorId)
        """;
        dbContext.Chapters.ExecuteDelete();

        foreach (SqliteDataReader dto in connection.Query(ancestorIdsQuery))
        {
            var ancestorId = GetAncestorId(dto);
            dbContext.AncestorIds.Add(ancestorId);
        }

        _logger.LogInformation("Try saving {0} AncestorIds entries.", dbContext.Chapters.Local.Count);

        dbContext.SaveChanges();
        migrationTotalTime += stopwatch.Elapsed;
        _logger.LogInformation("Saving AncestorIds took {0}.", stopwatch.Elapsed);
        stopwatch.Restart();

        connection.Close();
        _logger.LogInformation("Migration of the Library.db done.");
        _logger.LogInformation("Move {0} to {1}.", libraryDbPath, libraryDbPath + ".old");

        SqliteConnection.ClearAllPools();
        File.Move(libraryDbPath, libraryDbPath + ".old");

        _logger.LogInformation("Migrating Library db took {0}.", migrationTotalTime);

        if (dbContext.Database.IsSqlite())
        {
            _logger.LogInformation("Vacuum and Optimise jellyfin.db now.");
            dbContext.Database.ExecuteSqlRaw("PRAGMA optimize");
            dbContext.Database.ExecuteSqlRaw("VACUUM");
            _logger.LogInformation("jellyfin.db optimized successfully!");
        }
        else
        {
            _logger.LogInformation("This database doesn't support optimization");
        }
    }

    private UserData? GetUserData(ImmutableArray<User> users, SqliteDataReader dto)
    {
        var internalUserId = dto.GetInt32(1);
        var user = users.FirstOrDefault(e => e.InternalId == internalUserId);

        if (user is null)
        {
            _logger.LogError("Tried to find user with index '{Idx}' but there are only '{MaxIdx}' users.", internalUserId, users.Length);
            return null;
        }

        var oldKey = dto.GetString(0);

        return new UserData()
        {
            ItemId = Guid.NewGuid(),
            CustomDataKey = oldKey,
            UserId = user.Id,
            Rating = dto.IsDBNull(2) ? null : dto.GetDouble(2),
            Played = dto.GetBoolean(3),
            PlayCount = dto.GetInt32(4),
            IsFavorite = dto.GetBoolean(5),
            PlaybackPositionTicks = dto.GetInt64(6),
            LastPlayedDate = dto.IsDBNull(7) ? null : dto.GetDateTime(7),
            AudioStreamIndex = dto.IsDBNull(8) ? null : dto.GetInt32(8),
            SubtitleStreamIndex = dto.IsDBNull(9) ? null : dto.GetInt32(9),
            Likes = null,
            User = null!,
            Item = null!
        };
    }

    private AncestorId GetAncestorId(SqliteDataReader reader)
    {
        return new AncestorId()
        {
            ItemId = reader.GetGuid(0),
            ParentItemId = reader.GetGuid(1),
            Item = null!,
            ParentItem = null!
        };
    }

    /// <summary>
    /// Gets the chapter.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <returns>ChapterInfo.</returns>
    private Chapter GetChapter(SqliteDataReader reader)
    {
        var chapter = new Chapter
        {
            StartPositionTicks = reader.GetInt64(1),
            ChapterIndex = reader.GetInt32(5),
            Item = null!,
            ItemId = reader.GetGuid(0),
        };

        if (reader.TryGetString(2, out var chapterName))
        {
            chapter.Name = chapterName;
        }

        if (reader.TryGetString(3, out var imagePath))
        {
            chapter.ImagePath = imagePath;
        }

        if (reader.TryReadDateTime(4, out var imageDateModified))
        {
            chapter.ImageDateModified = imageDateModified;
        }

        return chapter;
    }

    private ItemValue GetItemValue(SqliteDataReader reader)
    {
        return new ItemValue
        {
            ItemValueId = Guid.NewGuid(),
            Type = (ItemValueType)reader.GetInt32(1),
            Value = reader.GetString(2),
            CleanValue = reader.GetString(3),
        };
    }

    private People GetPerson(SqliteDataReader reader)
    {
        var item = new People
        {
            Id = Guid.NewGuid(),
            Name = reader.GetString(1),
        };

        if (reader.TryGetString(3, out var type))
        {
            item.PersonType = type;
        }

        return item;
    }

    /// <summary>
    /// Gets the media stream.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <returns>MediaStream.</returns>
    private MediaStreamInfo GetMediaStream(SqliteDataReader reader)
    {
        var item = new MediaStreamInfo
        {
            StreamIndex = reader.GetInt32(1),
            StreamType = Enum.Parse<MediaStreamTypeEntity>(reader.GetString(2)),
            Item = null!,
            ItemId = reader.GetGuid(0),
            AspectRatio = null!,
            ChannelLayout = null!,
            Codec = null!,
            IsInterlaced = false,
            Language = null!,
            Path = null!,
            Profile = null!,
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
            item.Path = path;
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
            item.IsAvc = isAVC;
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

        // if (reader.TryGetInt32(44, out var rotation))
        // {
        //     item.Rotation = rotation;
        // }

        return item;
    }

    private (BaseItemEntity BaseItem, string[] LegacyUserDataKey) GetItem(SqliteDataReader reader)
    {
        var entity = new BaseItemEntity()
        {
            Id = reader.GetGuid(0),
            Type = reader.GetString(1),
        };

        var index = 2;

        if (reader.TryGetString(index++, out var data))
        {
            entity.Data = data;
        }

        if (reader.TryReadDateTime(index++, out var startDate))
        {
            entity.StartDate = startDate;
        }

        if (reader.TryReadDateTime(index++, out var endDate))
        {
            entity.EndDate = endDate;
        }

        if (reader.TryGetString(index++, out var guid))
        {
            entity.ChannelId = guid;
        }

        if (reader.TryGetBoolean(index++, out var isMovie))
        {
            entity.IsMovie = isMovie;
        }

        if (reader.TryGetBoolean(index++, out var isSeries))
        {
            entity.IsSeries = isSeries;
        }

        if (reader.TryGetString(index++, out var episodeTitle))
        {
            entity.EpisodeTitle = episodeTitle;
        }

        if (reader.TryGetBoolean(index++, out var isRepeat))
        {
            entity.IsRepeat = isRepeat;
        }

        if (reader.TryGetSingle(index++, out var communityRating))
        {
            entity.CommunityRating = communityRating;
        }

        if (reader.TryGetString(index++, out var customRating))
        {
            entity.CustomRating = customRating;
        }

        if (reader.TryGetInt32(index++, out var indexNumber))
        {
            entity.IndexNumber = indexNumber;
        }

        if (reader.TryGetBoolean(index++, out var isLocked))
        {
            entity.IsLocked = isLocked;
        }

        if (reader.TryGetString(index++, out var preferredMetadataLanguage))
        {
            entity.PreferredMetadataLanguage = preferredMetadataLanguage;
        }

        if (reader.TryGetString(index++, out var preferredMetadataCountryCode))
        {
            entity.PreferredMetadataCountryCode = preferredMetadataCountryCode;
        }

        if (reader.TryGetInt32(index++, out var width))
        {
            entity.Width = width;
        }

        if (reader.TryGetInt32(index++, out var height))
        {
            entity.Height = height;
        }

        if (reader.TryReadDateTime(index++, out var dateLastRefreshed))
        {
            entity.DateLastRefreshed = dateLastRefreshed;
        }

        if (reader.TryGetString(index++, out var name))
        {
            entity.Name = name;
        }

        if (reader.TryGetString(index++, out var restorePath))
        {
            entity.Path = restorePath;
        }

        if (reader.TryReadDateTime(index++, out var premiereDate))
        {
            entity.PremiereDate = premiereDate;
        }

        if (reader.TryGetString(index++, out var overview))
        {
            entity.Overview = overview;
        }

        if (reader.TryGetInt32(index++, out var parentIndexNumber))
        {
            entity.ParentIndexNumber = parentIndexNumber;
        }

        if (reader.TryGetInt32(index++, out var productionYear))
        {
            entity.ProductionYear = productionYear;
        }

        if (reader.TryGetString(index++, out var officialRating))
        {
            entity.OfficialRating = officialRating;
        }

        if (reader.TryGetString(index++, out var forcedSortName))
        {
            entity.ForcedSortName = forcedSortName;
        }

        if (reader.TryGetInt64(index++, out var runTimeTicks))
        {
            entity.RunTimeTicks = runTimeTicks;
        }

        if (reader.TryGetInt64(index++, out var size))
        {
            entity.Size = size;
        }

        if (reader.TryReadDateTime(index++, out var dateCreated))
        {
            entity.DateCreated = dateCreated;
        }

        if (reader.TryReadDateTime(index++, out var dateModified))
        {
            entity.DateModified = dateModified;
        }

        if (reader.TryGetString(index++, out var genres))
        {
            entity.Genres = genres;
        }

        if (reader.TryGetGuid(index++, out var parentId))
        {
            entity.ParentId = parentId;
        }

        if (reader.TryGetGuid(index++, out var topParentId))
        {
            entity.TopParentId = topParentId;
        }

        if (reader.TryGetString(index++, out var audioString) && Enum.TryParse<ProgramAudioEntity>(audioString, out var audioType))
        {
            entity.Audio = audioType;
        }

        if (reader.TryGetString(index++, out var serviceName))
        {
            entity.ExternalServiceId = serviceName;
        }

        if (reader.TryGetBoolean(index++, out var isInMixedFolder))
        {
            entity.IsInMixedFolder = isInMixedFolder;
        }

        if (reader.TryReadDateTime(index++, out var dateLastSaved))
        {
            entity.DateLastSaved = dateLastSaved;
        }

        if (reader.TryGetString(index++, out var lockedFields))
        {
            entity.LockedFields = lockedFields.Split('|').Select(Enum.Parse<MetadataField>)
                .Select(e => new BaseItemMetadataField()
                {
                    Id = (int)e,
                    Item = entity,
                    ItemId = entity.Id
                })
                .ToArray();
        }

        if (reader.TryGetString(index++, out var studios))
        {
            entity.Studios = studios;
        }

        if (reader.TryGetString(index++, out var tags))
        {
            entity.Tags = tags;
        }

        if (reader.TryGetString(index++, out var trailerTypes))
        {
            entity.TrailerTypes = trailerTypes.Split('|').Select(Enum.Parse<TrailerType>)
                .Select(e => new BaseItemTrailerType()
                {
                    Id = (int)e,
                    Item = entity,
                    ItemId = entity.Id
                })
                .ToArray();
        }

        if (reader.TryGetString(index++, out var originalTitle))
        {
            entity.OriginalTitle = originalTitle;
        }

        if (reader.TryGetString(index++, out var primaryVersionId))
        {
            entity.PrimaryVersionId = primaryVersionId;
        }

        if (reader.TryReadDateTime(index++, out var dateLastMediaAdded))
        {
            entity.DateLastMediaAdded = dateLastMediaAdded;
        }

        if (reader.TryGetString(index++, out var album))
        {
            entity.Album = album;
        }

        if (reader.TryGetSingle(index++, out var lUFS))
        {
            entity.LUFS = lUFS;
        }

        if (reader.TryGetSingle(index++, out var normalizationGain))
        {
            entity.NormalizationGain = normalizationGain;
        }

        if (reader.TryGetSingle(index++, out var criticRating))
        {
            entity.CriticRating = criticRating;
        }

        if (reader.TryGetBoolean(index++, out var isVirtualItem))
        {
            entity.IsVirtualItem = isVirtualItem;
        }

        if (reader.TryGetString(index++, out var seriesName))
        {
            entity.SeriesName = seriesName;
        }

        var userDataKeys = new List<string>();
        if (reader.TryGetString(index++, out var directUserDataKey))
        {
            userDataKeys.Add(directUserDataKey);
        }

        if (reader.TryGetString(index++, out var seasonName))
        {
            entity.SeasonName = seasonName;
        }

        if (reader.TryGetGuid(index++, out var seasonId))
        {
            entity.SeasonId = seasonId;
        }

        if (reader.TryGetGuid(index++, out var seriesId))
        {
            entity.SeriesId = seriesId;
        }

        if (reader.TryGetString(index++, out var presentationUniqueKey))
        {
            entity.PresentationUniqueKey = presentationUniqueKey;
        }

        if (reader.TryGetInt32(index++, out var parentalRating))
        {
            entity.InheritedParentalRatingValue = parentalRating;
        }

        if (reader.TryGetString(index++, out var externalSeriesId))
        {
            entity.ExternalSeriesId = externalSeriesId;
        }

        if (reader.TryGetString(index++, out var tagLine))
        {
            entity.Tagline = tagLine;
        }

        if (reader.TryGetString(index++, out var providerIds))
        {
            entity.Provider = providerIds.Split('|').Select(e => e.Split("="))
            .Select(e => new BaseItemProvider()
            {
                Item = null!,
                ProviderId = e[0],
                ProviderValue = e[1]
            }).ToArray();
        }

        if (reader.TryGetString(index++, out var imageInfos))
        {
            entity.Images = DeserializeImages(imageInfos).Select(f => Map(entity.Id, f)).ToArray();
        }

        if (reader.TryGetString(index++, out var productionLocations))
        {
            entity.ProductionLocations = productionLocations;
        }

        if (reader.TryGetString(index++, out var extraIds))
        {
            entity.ExtraIds = extraIds;
        }

        if (reader.TryGetInt32(index++, out var totalBitrate))
        {
            entity.TotalBitrate = totalBitrate;
        }

        if (reader.TryGetString(index++, out var extraTypeString) && Enum.TryParse<BaseItemExtraType>(extraTypeString, out var extraType))
        {
            entity.ExtraType = extraType;
        }

        if (reader.TryGetString(index++, out var artists))
        {
            entity.Artists = artists;
        }

        if (reader.TryGetString(index++, out var albumArtists))
        {
            entity.AlbumArtists = albumArtists;
        }

        if (reader.TryGetString(index++, out var externalId))
        {
            entity.ExternalId = externalId;
        }

        if (reader.TryGetString(index++, out var seriesPresentationUniqueKey))
        {
            entity.SeriesPresentationUniqueKey = seriesPresentationUniqueKey;
        }

        if (reader.TryGetString(index++, out var showId))
        {
            entity.ShowId = showId;
        }

        if (reader.TryGetString(index++, out var ownerId))
        {
            entity.OwnerId = ownerId;
        }

        if (reader.TryGetString(index++, out var mediaType))
        {
            entity.MediaType = mediaType;
        }

        var baseItem = BaseItemRepository.DeserialiseBaseItem(entity, _logger, null, false);
        var dataKeys = baseItem.GetUserDataKeys();
        userDataKeys.AddRange(dataKeys);

        return (entity, userDataKeys.ToArray());
    }

    private static BaseItemImageInfo Map(Guid baseItemId, ItemImageInfo e)
    {
        return new BaseItemImageInfo()
        {
            ItemId = baseItemId,
            Id = Guid.NewGuid(),
            Path = e.Path,
            Blurhash = e.BlurHash != null ? Encoding.UTF8.GetBytes(e.BlurHash) : null,
            DateModified = e.DateModified,
            Height = e.Height,
            Width = e.Width,
            ImageType = (ImageInfoImageType)e.Type,
            Item = null!
        };
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

            if (image is not null)
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

    internal ItemImageInfo? ItemImageInfoFromValueString(ReadOnlySpan<char> value)
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
            Path = path.ToString()
        };

        if (long.TryParse(dateModified, CultureInfo.InvariantCulture, out var ticks)
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
}
