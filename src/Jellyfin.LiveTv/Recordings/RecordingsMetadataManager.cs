using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using Jellyfin.LiveTv.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.LiveTv.Recordings;

/// <summary>
/// A service responsible for saving recording metadata.
/// </summary>
public class RecordingsMetadataManager
{
    private const string DateAddedFormat = "yyyy-MM-dd HH:mm:ss";

    private readonly ILogger<RecordingsMetadataManager> _logger;
    private readonly IConfigurationManager _config;
    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecordingsMetadataManager"/> class.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    /// <param name="config">The <see cref="IConfigurationManager"/>.</param>
    /// <param name="libraryManager">The <see cref="ILibraryManager"/>.</param>
    public RecordingsMetadataManager(
        ILogger<RecordingsMetadataManager> logger,
        IConfigurationManager config,
        ILibraryManager libraryManager)
    {
        _logger = logger;
        _config = config;
        _libraryManager = libraryManager;
    }

    /// <summary>
    /// Saves the metadata for a provided recording.
    /// </summary>
    /// <param name="timer">The recording timer.</param>
    /// <param name="recordingPath">The recording path.</param>
    /// <param name="seriesPath">The series path.</param>
    /// <returns>A task representing the metadata saving.</returns>
    public async Task SaveRecordingMetadata(TimerInfo timer, string recordingPath, string? seriesPath)
    {
        try
        {
            var program = string.IsNullOrWhiteSpace(timer.ProgramId) ? null : _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = [BaseItemKind.LiveTvProgram],
                Limit = 1,
                ExternalId = timer.ProgramId,
                DtoOptions = new DtoOptions(true)
            }).FirstOrDefault() as LiveTvProgram;

            // dummy this up
            program ??= new LiveTvProgram
            {
                Name = timer.Name,
                Overview = timer.Overview,
                Genres = timer.Genres,
                CommunityRating = timer.CommunityRating,
                OfficialRating = timer.OfficialRating,
                ProductionYear = timer.ProductionYear,
                PremiereDate = timer.OriginalAirDate,
                IndexNumber = timer.EpisodeNumber,
                ParentIndexNumber = timer.SeasonNumber
            };

            if (timer.IsSports)
            {
                program.AddGenre("Sports");
            }

            if (timer.IsKids)
            {
                program.AddGenre("Kids");
                program.AddGenre("Children");
            }

            if (timer.IsNews)
            {
                program.AddGenre("News");
            }

            var config = _config.GetLiveTvConfiguration();

            if (config.SaveRecordingNFO)
            {
                if (timer.IsProgramSeries)
                {
                    ArgumentNullException.ThrowIfNull(seriesPath);

                    await SaveSeriesNfoAsync(timer, seriesPath).ConfigureAwait(false);
                    await SaveVideoNfoAsync(timer, recordingPath, program, false).ConfigureAwait(false);
                }
                else if (!timer.IsMovie || timer.IsSports || timer.IsNews)
                {
                    await SaveVideoNfoAsync(timer, recordingPath, program, true).ConfigureAwait(false);
                }
                else
                {
                    await SaveVideoNfoAsync(timer, recordingPath, program, false).ConfigureAwait(false);
                }
            }

            if (config.SaveRecordingImages)
            {
                await SaveRecordingImages(recordingPath, program).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving nfo");
        }
    }

    private static async Task SaveSeriesNfoAsync(TimerInfo timer, string seriesPath)
    {
        var nfoPath = Path.Combine(seriesPath, "tvshow.nfo");

        if (File.Exists(nfoPath))
        {
            return;
        }

        var stream = new FileStream(nfoPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await using (stream.ConfigureAwait(false))
        {
            var settings = new XmlWriterSettings
            {
                Indent = true,
                Encoding = Encoding.UTF8,
                Async = true
            };

            var writer = XmlWriter.Create(stream, settings);
            await using (writer.ConfigureAwait(false))
            {
                await writer.WriteStartDocumentAsync(true).ConfigureAwait(false);
                await writer.WriteStartElementAsync(null, "tvshow", null).ConfigureAwait(false);
                if (timer.SeriesProviderIds.TryGetValue(MetadataProvider.Tvdb.ToString(), out var id))
                {
                    await writer.WriteElementStringAsync(null, "id", null, id).ConfigureAwait(false);
                }

                if (timer.SeriesProviderIds.TryGetValue(MetadataProvider.Imdb.ToString(), out id))
                {
                    await writer.WriteElementStringAsync(null, "imdb_id", null, id).ConfigureAwait(false);
                }

                if (timer.SeriesProviderIds.TryGetValue(MetadataProvider.Tmdb.ToString(), out id))
                {
                    await writer.WriteElementStringAsync(null, "tmdbid", null, id).ConfigureAwait(false);
                }

                if (timer.SeriesProviderIds.TryGetValue(MetadataProvider.Zap2It.ToString(), out id))
                {
                    await writer.WriteElementStringAsync(null, "zap2itid", null, id).ConfigureAwait(false);
                }

                if (!string.IsNullOrWhiteSpace(timer.Name))
                {
                    await writer.WriteElementStringAsync(null, "title", null, timer.Name).ConfigureAwait(false);
                }

                if (!string.IsNullOrWhiteSpace(timer.OfficialRating))
                {
                    await writer.WriteElementStringAsync(null, "mpaa", null, timer.OfficialRating).ConfigureAwait(false);
                }

                foreach (var genre in timer.Genres)
                {
                    await writer.WriteElementStringAsync(null, "genre", null, genre).ConfigureAwait(false);
                }

                await writer.WriteEndElementAsync().ConfigureAwait(false);
                await writer.WriteEndDocumentAsync().ConfigureAwait(false);
            }
        }
    }

    private async Task SaveVideoNfoAsync(TimerInfo timer, string recordingPath, BaseItem item, bool lockData)
    {
        var nfoPath = Path.ChangeExtension(recordingPath, ".nfo");

        if (File.Exists(nfoPath))
        {
            return;
        }

        var stream = new FileStream(nfoPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await using (stream.ConfigureAwait(false))
        {
            var settings = new XmlWriterSettings
            {
                Indent = true,
                Encoding = Encoding.UTF8,
                Async = true
            };

            var options = _config.GetNfoConfiguration();

            var isSeriesEpisode = timer.IsProgramSeries;

            var writer = XmlWriter.Create(stream, settings);
            await using (writer.ConfigureAwait(false))
            {
                await writer.WriteStartDocumentAsync(true).ConfigureAwait(false);

                if (isSeriesEpisode)
                {
                    await writer.WriteStartElementAsync(null, "episodedetails", null).ConfigureAwait(false);

                    if (!string.IsNullOrWhiteSpace(timer.EpisodeTitle))
                    {
                        await writer.WriteElementStringAsync(null, "title", null, timer.EpisodeTitle).ConfigureAwait(false);
                    }

                    var premiereDate = item.PremiereDate ?? (!timer.IsRepeat ? DateTime.UtcNow : null);

                    if (premiereDate.HasValue)
                    {
                        var formatString = options.ReleaseDateFormat;

                        await writer.WriteElementStringAsync(
                            null,
                            "aired",
                            null,
                            premiereDate.Value.ToLocalTime().ToString(formatString, CultureInfo.InvariantCulture)).ConfigureAwait(false);
                    }

                    if (item.IndexNumber.HasValue)
                    {
                        await writer.WriteElementStringAsync(null, "episode", null, item.IndexNumber.Value.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);
                    }

                    if (item.ParentIndexNumber.HasValue)
                    {
                        await writer.WriteElementStringAsync(null, "season", null, item.ParentIndexNumber.Value.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);
                    }
                }
                else
                {
                    await writer.WriteStartElementAsync(null, "movie", null).ConfigureAwait(false);

                    if (!string.IsNullOrWhiteSpace(item.Name))
                    {
                        await writer.WriteElementStringAsync(null, "title", null, item.Name).ConfigureAwait(false);
                    }

                    if (!string.IsNullOrWhiteSpace(item.OriginalTitle))
                    {
                        await writer.WriteElementStringAsync(null, "originaltitle", null, item.OriginalTitle).ConfigureAwait(false);
                    }

                    if (item.PremiereDate.HasValue)
                    {
                        var formatString = options.ReleaseDateFormat;

                        await writer.WriteElementStringAsync(
                            null,
                            "premiered",
                            null,
                            item.PremiereDate.Value.ToLocalTime().ToString(formatString, CultureInfo.InvariantCulture)).ConfigureAwait(false);
                        await writer.WriteElementStringAsync(
                            null,
                            "releasedate",
                            null,
                            item.PremiereDate.Value.ToLocalTime().ToString(formatString, CultureInfo.InvariantCulture)).ConfigureAwait(false);
                    }
                }

                await writer.WriteElementStringAsync(
                    null,
                    "dateadded",
                    null,
                    DateTime.Now.ToString(DateAddedFormat, CultureInfo.InvariantCulture)).ConfigureAwait(false);

                if (item.ProductionYear.HasValue)
                {
                    await writer.WriteElementStringAsync(null, "year", null, item.ProductionYear.Value.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);
                }

                if (!string.IsNullOrEmpty(item.OfficialRating))
                {
                    await writer.WriteElementStringAsync(null, "mpaa", null, item.OfficialRating).ConfigureAwait(false);
                }

                var overview = (item.Overview ?? string.Empty)
                    .StripHtml()
                    .Replace("&quot;", "'", StringComparison.Ordinal);

                await writer.WriteElementStringAsync(null, "plot", null, overview).ConfigureAwait(false);

                if (item.CommunityRating.HasValue)
                {
                    await writer.WriteElementStringAsync(null, "rating", null, item.CommunityRating.Value.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);
                }

                foreach (var genre in item.Genres)
                {
                    await writer.WriteElementStringAsync(null, "genre", null, genre).ConfigureAwait(false);
                }

                var people = item.Id.IsEmpty() ? new List<PersonInfo>() : _libraryManager.GetPeople(item);

                var directors = people
                    .Where(i => i.IsType(PersonKind.Director))
                    .Select(i => i.Name)
                    .ToList();

                foreach (var person in directors)
                {
                    await writer.WriteElementStringAsync(null, "director", null, person).ConfigureAwait(false);
                }

                var writers = people
                    .Where(i => i.IsType(PersonKind.Writer))
                    .Select(i => i.Name)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var person in writers)
                {
                    await writer.WriteElementStringAsync(null, "writer", null, person).ConfigureAwait(false);
                }

                foreach (var person in writers)
                {
                    await writer.WriteElementStringAsync(null, "credits", null, person).ConfigureAwait(false);
                }

                var tmdbCollection = item.GetProviderId(MetadataProvider.TmdbCollection);

                if (!string.IsNullOrEmpty(tmdbCollection))
                {
                    await writer.WriteElementStringAsync(null, "collectionnumber", null, tmdbCollection).ConfigureAwait(false);
                }

                var imdb = item.GetProviderId(MetadataProvider.Imdb);
                if (!string.IsNullOrEmpty(imdb))
                {
                    if (!isSeriesEpisode)
                    {
                        await writer.WriteElementStringAsync(null, "id", null, imdb).ConfigureAwait(false);
                    }

                    await writer.WriteElementStringAsync(null, "imdbid", null, imdb).ConfigureAwait(false);

                    // No need to lock if we have identified the content already
                    lockData = false;
                }

                var tvdb = item.GetProviderId(MetadataProvider.Tvdb);
                if (!string.IsNullOrEmpty(tvdb))
                {
                    await writer.WriteElementStringAsync(null, "tvdbid", null, tvdb).ConfigureAwait(false);

                    // No need to lock if we have identified the content already
                    lockData = false;
                }

                var tmdb = item.GetProviderId(MetadataProvider.Tmdb);
                if (!string.IsNullOrEmpty(tmdb))
                {
                    await writer.WriteElementStringAsync(null, "tmdbid", null, tmdb).ConfigureAwait(false);

                    // No need to lock if we have identified the content already
                    lockData = false;
                }

                if (lockData)
                {
                    await writer.WriteElementStringAsync(null, "lockdata", null, "true").ConfigureAwait(false);
                }

                if (item.CriticRating.HasValue)
                {
                    await writer.WriteElementStringAsync(null, "criticrating", null, item.CriticRating.Value.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);
                }

                if (!string.IsNullOrWhiteSpace(item.Tagline))
                {
                    await writer.WriteElementStringAsync(null, "tagline", null, item.Tagline).ConfigureAwait(false);
                }

                foreach (var studio in item.Studios)
                {
                    await writer.WriteElementStringAsync(null, "studio", null, studio).ConfigureAwait(false);
                }

                await writer.WriteEndElementAsync().ConfigureAwait(false);
                await writer.WriteEndDocumentAsync().ConfigureAwait(false);
            }
        }
    }

    private async Task SaveRecordingImages(string recordingPath, LiveTvProgram program)
    {
        var image = program.IsSeries ?
            (program.GetImageInfo(ImageType.Thumb, 0) ?? program.GetImageInfo(ImageType.Primary, 0)) :
            (program.GetImageInfo(ImageType.Primary, 0) ?? program.GetImageInfo(ImageType.Thumb, 0));

        if (image is not null)
        {
            try
            {
                await SaveRecordingImage(recordingPath, program, image).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving recording image");
            }
        }

        if (!program.IsSeries)
        {
            image = program.GetImageInfo(ImageType.Backdrop, 0);
            if (image is not null)
            {
                try
                {
                    await SaveRecordingImage(recordingPath, program, image).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving recording image");
                }
            }

            image = program.GetImageInfo(ImageType.Thumb, 0);
            if (image is not null)
            {
                try
                {
                    await SaveRecordingImage(recordingPath, program, image).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving recording image");
                }
            }

            image = program.GetImageInfo(ImageType.Logo, 0);
            if (image is not null)
            {
                try
                {
                    await SaveRecordingImage(recordingPath, program, image).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving recording image");
                }
            }
        }
    }

    private async Task SaveRecordingImage(string recordingPath, LiveTvProgram program, ItemImageInfo image)
    {
        if (!image.IsLocalFile)
        {
            image = await _libraryManager.ConvertImageToLocal(program, image, 0).ConfigureAwait(false);
        }

        var imageSaveFilenameWithoutExtension = image.Type switch
        {
            ImageType.Primary => program.IsSeries ? Path.GetFileNameWithoutExtension(recordingPath) + "-thumb" : "poster",
            ImageType.Logo => "logo",
            ImageType.Thumb => program.IsSeries ? Path.GetFileNameWithoutExtension(recordingPath) + "-thumb" : "landscape",
            ImageType.Backdrop => "fanart",
            _ => null
        };

        if (imageSaveFilenameWithoutExtension is null)
        {
            return;
        }

        var imageSavePath = Path.Combine(Path.GetDirectoryName(recordingPath)!, imageSaveFilenameWithoutExtension);

        // preserve original image extension
        imageSavePath = Path.ChangeExtension(imageSavePath, Path.GetExtension(image.Path));

        File.Copy(image.Path, imageSavePath, true);
    }
}
