using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Entities.Libraries;
using Jellyfin.Server.Implementations;
using MediaBrowser.Controller;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.LiveTv;
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

    private readonly ILogger<MigrateUserDb> _logger;
    private readonly IServerApplicationPaths _paths;
    private readonly IDbContextFactory<JellyfinDbContext> _provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrateLibraryDb"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="provider">The database provider.</param>
    /// <param name="paths">The server application paths.</param>
    public MigrateLibraryDb(
        ILogger<MigrateUserDb> logger,
        IDbContextFactory<JellyfinDbContext> provider,
        IServerApplicationPaths paths)
    {
        _logger = logger;
        _provider = provider;
        _paths = paths;
    }

    /// <inheritdoc/>
    public Guid Id => Guid.Parse("5bcb4197-e7c0-45aa-9902-963bceab5798");

    /// <inheritdoc/>
    public string Name => "MigrateUserData";

    /// <inheritdoc/>
    public bool PerformOnNewInstall => false;

    /// <inheritdoc/>
    public void Perform()
    {
        _logger.LogInformation("Migrating the userdata from library.db may take a while, do not stop Jellyfin.");

        var dataPath = _paths.DataPath;
        var libraryDbPath = Path.Combine(dataPath, DbFilename);
        using var connection = new SqliteConnection($"Filename={libraryDbPath}");

        connection.Open();
        using var dbContext = _provider.CreateDbContext();

        var queryResult = connection.Query("SELECT key, userId, rating, played, playCount, isFavorite, playbackPositionTicks, lastPlayedDate, AudioStreamIndex, SubtitleStreamIndex FROM UserDatas");

        dbContext.UserData.ExecuteDelete();

        var users = dbContext.Users.AsNoTracking().ToImmutableArray();

        foreach (SqliteDataReader dto in queryResult)
        {
            dbContext.UserData.Add(GetUserData(users, dto));
        }

        dbContext.SaveChanges();

        var typedBaseItemsQuery = "SELECT type, data, StartDate, EndDate, ChannelId, IsMovie, IsSeries, EpisodeTitle, IsRepeat, CommunityRating, CustomRating, IndexNumber, IsLocked, PreferredMetadataLanguage, PreferredMetadataCountryCode, Width, Height, DateLastRefreshed, Name, Path, PremiereDate, Overview, ParentIndexNumber, ProductionYear, OfficialRating, ForcedSortName, RunTimeTicks, Size, DateCreated, DateModified, guid, Genres, ParentId, Audio, ExternalServiceId, IsInMixedFolder, DateLastSaved, LockedFields, Studios, Tags, TrailerTypes, OriginalTitle, PrimaryVersionId, DateLastMediaAdded, Album, LUFS, NormalizationGain, CriticRating, IsVirtualItem, SeriesName, SeasonName, SeasonId, SeriesId, PresentationUniqueKey, InheritedParentalRatingValue, ExternalSeriesId, Tagline, ProviderIds, Images, ProductionLocations, ExtraIds, TotalBitrate, ExtraType, Artists, AlbumArtists, ExternalId, SeriesPresentationUniqueKey, ShowId, OwnerId FROM TypeBaseItems";
        dbContext.BaseItems.ExecuteDelete();

        foreach (SqliteDataReader dto in connection.Query(typedBaseItemsQuery))
        {
            dbContext.BaseItems.Add(GetItem(dto));
        }

        dbContext.SaveChanges();

        var mediaStreamQuery = "SELECT ItemId, StreamIndex, StreamType, Codec, Language, ChannelLayout, Profile, AspectRatio, Path, IsInterlaced, BitRate, Channels, SampleRate, IsDefault, IsForced, IsExternal, Height, Width, AverageFrameRate, RealFrameRate, Level, PixelFormat, BitDepth, IsAnamorphic, RefFrames, CodecTag, Comment, NalLengthSize, IsAvc, Title, TimeBase, CodecTimeBase, ColorPrimaries, ColorSpace, ColorTransfer, DvVersionMajor, DvVersionMinor, DvProfile, DvLevel, RpuPresentFlag, ElPresentFlag, BlPresentFlag, DvBlSignalCompatibilityId, IsHearingImpaired, Rotation FROM MediaStreams";
        dbContext.MediaStreamInfos.ExecuteDelete();

        foreach (SqliteDataReader dto in connection.Query(mediaStreamQuery))
        {
            dbContext.MediaStreamInfos.Add(GetMediaStream(dto));
        }

        dbContext.SaveChanges();

        var personsQuery = "select ItemId, Name, Role, PersonType, SortOrder from People p";
        dbContext.Peoples.ExecuteDelete();

        foreach (SqliteDataReader dto in connection.Query(personsQuery))
        {
            dbContext.Peoples.Add(GetPerson(dto));
        }

        dbContext.SaveChanges();

        var itemValueQuery = "select ItemId, Type, Value, CleanValue FROM ItemValues";
        dbContext.ItemValues.ExecuteDelete();

        foreach (SqliteDataReader dto in connection.Query(itemValueQuery))
        {
            dbContext.ItemValues.Add(GetItemValue(dto));
        }

        dbContext.SaveChanges();

        var chapterQuery = "select StartPositionTicks,Name,ImagePath,ImageDateModified from Chapters2";
        dbContext.Chapters.ExecuteDelete();

        foreach (SqliteDataReader dto in connection.Query(chapterQuery))
        {
            dbContext.Chapters.Add(GetChapter(dto));
        }

        dbContext.SaveChanges();

        var ancestorIdsQuery = "select ItemId, AncestorId, AncestorIdText from AncestorIds";
        dbContext.Chapters.ExecuteDelete();

        foreach (SqliteDataReader dto in connection.Query(ancestorIdsQuery))
        {
            dbContext.AncestorIds.Add(GetAncestorId(dto));
        }

        dbContext.SaveChanges();

        connection.Close();
        _logger.LogInformation("Migration of the Library.db done.");
        _logger.LogInformation("Move {0} to {1}.", libraryDbPath, libraryDbPath + ".old");
        File.Move(libraryDbPath, libraryDbPath + ".old");

        if (dbContext.Database.IsSqlite())
        {
            _logger.LogInformation("Vaccum and Optimise jellyfin.db now.");
            dbContext.Database.ExecuteSqlRaw("PRAGMA optimize");
            dbContext.Database.ExecuteSqlRaw("VACUUM");
            _logger.LogInformation("jellyfin.db optimized successfully!");
        }
        else
        {
            _logger.LogInformation("This database doesn't support optimization");
        }
    }

    private static UserData GetUserData(ImmutableArray<User> users, SqliteDataReader dto)
    {
        return new UserData()
        {
            Key = dto.GetString(0),
            UserId = users.ElementAt(dto.GetInt32(1)).Id,
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
        };
    }

    private AncestorId GetAncestorId(SqliteDataReader reader)
    {
        return new AncestorId()
        {
            Item = null!,
            ItemId = reader.GetGuid(0),
            Id = reader.GetGuid(1),
            AncestorIdText = reader.GetString(2)
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
            StartPositionTicks = reader.GetInt64(0),
            ChapterIndex = 0,
            Item = null!,
            ItemId = Guid.Empty
        };

        if (reader.TryGetString(1, out var chapterName))
        {
            chapter.Name = chapterName;
        }

        if (reader.TryGetString(2, out var imagePath))
        {
            chapter.ImagePath = imagePath;
        }

        if (reader.TryReadDateTime(3, out var imageDateModified))
        {
            chapter.ImageDateModified = imageDateModified;
        }

        return chapter;
    }

    private ItemValue GetItemValue(SqliteDataReader reader)
    {
        return new ItemValue
        {
            ItemId = reader.GetGuid(0),
            Type = reader.GetInt32(1),
            Value = reader.GetString(2),
            CleanValue = reader.GetString(3),
            Item = null!
        };
    }

    private People GetPerson(SqliteDataReader reader)
    {
        var item = new People
        {
            ItemId = reader.GetGuid(0),
            Name = reader.GetString(1),
            Item = null!
        };

        if (reader.TryGetString(2, out var role))
        {
            item.Role = role;
        }

        if (reader.TryGetString(3, out var type))
        {
            item.PersonType = type;
        }

        if (reader.TryGetInt32(4, out var sortOrder))
        {
            item.SortOrder = sortOrder;
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
            StreamType = reader.GetString(2),
            Item = null!,
            ItemId = reader.GetGuid(0),
            AverageFrameRate = 0,
            BitDepth = 0,
            BitRate = 0,
            BlPresentFlag = 0,
            Channels = 0,
            CodecTag = string.Empty,
            CodecTimeBase = string.Empty,
            ColorPrimaries = string.Empty,
            ColorSpace = string.Empty,
            ColorTransfer = string.Empty,
            Comment = string.Empty,
            DvBlSignalCompatibilityId = 0,
            DvLevel = 0,
            DvProfile = 0,
            DvVersionMajor = 0,
            DvVersionMinor = 0,
            ElPresentFlag = 0,
            Height = 0,
            IsAnamorphic = false,
            IsAvc = false,
            IsHearingImpaired = false,
            Level = 0,
            NalLengthSize = string.Empty,
            RealFrameRate = 0,
            RefFrames = 0,
            Rotation = 0,
            RpuPresentFlag = 0,
            SampleRate = 0,
            TimeBase = string.Empty,
            Title = string.Empty,
            Width = 0
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

        if (reader.TryGetInt32(44, out var rotation))
        {
            item.Rotation = rotation;
        }

        return item;
    }

    private BaseItemEntity GetItem(SqliteDataReader reader)
    {
        var item = new BaseItemEntity()
        {
            Type = reader.GetString(0)
        };

        var index = 1;

        if (reader.TryGetString(index++, out var data))
        {
            item.Data = data;
        }

        if (reader.TryReadDateTime(index++, out var startDate))
        {
            item.StartDate = startDate;
        }

        if (reader.TryReadDateTime(index++, out var endDate))
        {
            item.EndDate = endDate;
        }

        if (reader.TryGetGuid(index++, out var guid))
        {
            item.ChannelId = guid.ToString("N");
        }

        if (reader.TryGetBoolean(index++, out var isMovie))
        {
            item.IsMovie = isMovie;
        }

        if (reader.TryGetBoolean(index++, out var isSeries))
        {
            item.IsSeries = isSeries;
        }

        if (reader.TryGetString(index++, out var episodeTitle))
        {
            item.EpisodeTitle = episodeTitle;
        }

        if (reader.TryGetBoolean(index++, out var isRepeat))
        {
            item.IsRepeat = isRepeat;
        }

        if (reader.TryGetSingle(index++, out var communityRating))
        {
            item.CommunityRating = communityRating;
        }

        if (reader.TryGetString(index++, out var customRating))
        {
            item.CustomRating = customRating;
        }

        if (reader.TryGetInt32(index++, out var indexNumber))
        {
            item.IndexNumber = indexNumber;
        }

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

        if (reader.TryGetInt32(index++, out var width))
        {
            item.Width = width;
        }

        if (reader.TryGetInt32(index++, out var height))
        {
            item.Height = height;
        }

        if (reader.TryReadDateTime(index++, out var dateLastRefreshed))
        {
            item.DateLastRefreshed = dateLastRefreshed;
        }

        if (reader.TryGetString(index++, out var name))
        {
            item.Name = name;
        }

        if (reader.TryGetString(index++, out var restorePath))
        {
            item.Path = restorePath;
        }

        if (reader.TryReadDateTime(index++, out var premiereDate))
        {
            item.PremiereDate = premiereDate;
        }

        if (reader.TryGetString(index++, out var overview))
        {
            item.Overview = overview;
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

        if (reader.TryGetString(index++, out var forcedSortName))
        {
            item.ForcedSortName = forcedSortName;
        }

        if (reader.TryGetInt64(index++, out var runTimeTicks))
        {
            item.RunTimeTicks = runTimeTicks;
        }

        if (reader.TryGetInt64(index++, out var size))
        {
            item.Size = size;
        }

        if (reader.TryReadDateTime(index++, out var dateCreated))
        {
            item.DateCreated = dateCreated;
        }

        if (reader.TryReadDateTime(index++, out var dateModified))
        {
            item.DateModified = dateModified;
        }

        item.Id = reader.GetGuid(index++);

        if (reader.TryGetString(index++, out var genres))
        {
            item.Genres = genres;
        }

        if (reader.TryGetGuid(index++, out var parentId))
        {
            item.ParentId = parentId;
        }

        if (reader.TryGetString(index++, out var audioString))
        {
            item.Audio = audioString;
        }

        if (reader.TryGetString(index++, out var serviceName))
        {
            item.ExternalServiceId = serviceName;
        }

        if (reader.TryGetBoolean(index++, out var isInMixedFolder))
        {
            item.IsInMixedFolder = isInMixedFolder;
        }

        if (reader.TryReadDateTime(index++, out var dateLastSaved))
        {
            item.DateLastSaved = dateLastSaved;
        }

        if (reader.TryGetString(index++, out var lockedFields))
        {
            item.LockedFields = lockedFields;
        }

        if (reader.TryGetString(index++, out var studios))
        {
            item.Studios = studios;
        }

        if (reader.TryGetString(index++, out var tags))
        {
            item.Tags = tags;
        }

        if (reader.TryGetString(index++, out var trailerTypes))
        {
            item.TrailerTypes = trailerTypes;
        }

        if (reader.TryGetString(index++, out var originalTitle))
        {
            item.OriginalTitle = originalTitle;
        }

        if (reader.TryGetString(index++, out var primaryVersionId))
        {
            item.PrimaryVersionId = primaryVersionId;
        }

        if (reader.TryReadDateTime(index++, out var dateLastMediaAdded))
        {
            item.DateLastMediaAdded = dateLastMediaAdded;
        }

        if (reader.TryGetString(index++, out var album))
        {
            item.Album = album;
        }

        if (reader.TryGetSingle(index++, out var lUFS))
        {
            item.LUFS = lUFS;
        }

        if (reader.TryGetSingle(index++, out var normalizationGain))
        {
            item.NormalizationGain = normalizationGain;
        }

        if (reader.TryGetSingle(index++, out var criticRating))
        {
            item.CriticRating = criticRating;
        }

        if (reader.TryGetBoolean(index++, out var isVirtualItem))
        {
            item.IsVirtualItem = isVirtualItem;
        }

        if (reader.TryGetString(index++, out var seriesName))
        {
            item.SeriesName = seriesName;
        }

        if (reader.TryGetString(index++, out var seasonName))
        {
            item.SeasonName = seasonName;
        }

        if (reader.TryGetGuid(index++, out var seasonId))
        {
            item.SeasonId = seasonId;
        }

        if (reader.TryGetGuid(index++, out var seriesId))
        {
            item.SeriesId = seriesId;
        }

        if (reader.TryGetString(index++, out var presentationUniqueKey))
        {
            item.PresentationUniqueKey = presentationUniqueKey;
        }

        if (reader.TryGetInt32(index++, out var parentalRating))
        {
            item.InheritedParentalRatingValue = parentalRating;
        }

        if (reader.TryGetString(index++, out var externalSeriesId))
        {
            item.ExternalSeriesId = externalSeriesId;
        }

        if (reader.TryGetString(index++, out var tagLine))
        {
            item.Tagline = tagLine;
        }

        if (reader.TryGetString(index++, out var providerIds))
        {
            item.Provider = providerIds.Split('|').Select(e => e.Split("="))
            .Select(e => new BaseItemProvider()
            {
                Item = null!,
                ProviderId = e[0],
                ProviderValue = e[1]
            }).ToArray();
        }

        if (reader.TryGetString(index++, out var imageInfos))
        {
            item.Images = imageInfos;
        }

        if (reader.TryGetString(index++, out var productionLocations))
        {
            item.ProductionLocations = productionLocations;
        }

        if (reader.TryGetString(index++, out var extraIds))
        {
            item.ExtraIds = extraIds;
        }

        if (reader.TryGetInt32(index++, out var totalBitrate))
        {
            item.TotalBitrate = totalBitrate;
        }

        if (reader.TryGetString(index++, out var extraTypeString))
        {
            item.ExtraType = extraTypeString;
        }

        if (reader.TryGetString(index++, out var artists))
        {
            item.Artists = artists;
        }

        if (reader.TryGetString(index++, out var albumArtists))
        {
            item.AlbumArtists = albumArtists;
        }

        if (reader.TryGetString(index++, out var externalId))
        {
            item.ExternalId = externalId;
        }

        if (reader.TryGetString(index++, out var seriesPresentationUniqueKey))
        {
            item.SeriesPresentationUniqueKey = seriesPresentationUniqueKey;
        }

        if (reader.TryGetString(index++, out var showId))
        {
            item.ShowId = showId;
        }

        if (reader.TryGetGuid(index++, out var ownerId))
        {
            item.OwnerId = ownerId.ToString("N");
        }

        return item;
    }
}
