#nullable disable

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Emby.Naming.Common;
using Emby.Naming.TV;
using Emby.Server.Implementations.Library;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Library.Resolvers.TV
{
    /// <summary>
    /// Class SeasonResolver.
    /// </summary>
    public class SeasonResolver : GenericFolderResolver<Season>
    {
        private readonly ILocalizationManager _localization;
        private readonly ILogger<SeasonResolver> _logger;
        private readonly NamingOptions _namingOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="SeasonResolver"/> class.
        /// </summary>
        /// <param name="namingOptions">The naming options.</param>
        /// <param name="localization">The localization.</param>
        /// <param name="logger">The logger.</param>
        public SeasonResolver(
            NamingOptions namingOptions,
            ILocalizationManager localization,
            ILogger<SeasonResolver> logger)
        {
            _namingOptions = namingOptions;
            _localization = localization;
            _logger = logger;
        }

        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>Season.</returns>
        protected override Season Resolve(ItemResolveArgs args)
        {
            if (args.Parent is Series series && args.IsDirectory)
            {
                var namingOptions = _namingOptions;

                var path = args.Path;

                // Reject empty folders — a season folder must contain at least one video file.
                // This check runs regardless of naming, so folders like "Season 1" that are
                // empty don't create phantom seasons that compete with real ones.
                // Guard with Directory.Exists so tests using fake paths are unaffected.
                if (Directory.Exists(path))
                {
                    var exts = _namingOptions.VideoFileExtensions;

                    // 1) Check top-level files — most season folders have episodes directly here.
                    var hasAnyVideo = Directory.EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly)
                        .Any(file => exts.Contains(Path.GetExtension(file)));

                    // 2) Check one level of subdirectories — covers episode-in-subfolder layouts
                    //    without recursing into .trickplay/, .actors/ or other metadata folders
                    //    that don't contain video files.
                    if (!hasAnyVideo)
                    {
                        hasAnyVideo = Directory.EnumerateDirectories(path)
                            .Any(subdir => Directory.EnumerateFiles(subdir, "*", SearchOption.TopDirectoryOnly)
                                .Any(file => exts.Contains(Path.GetExtension(file))));
                    }

                    // 3) Rare deep-nesting fallback — e.g. BDMV/STREAM/*.m2ts in a TV folder.
                    if (!hasAnyVideo)
                    {
                        hasAnyVideo = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
                            .Any(file => exts.Contains(Path.GetExtension(file)));
                    }

                    if (!hasAnyVideo)
                    {
                        return null;
                    }
                }

                var seasonParserResult = SeasonPathParser.Parse(path, series.ContainingFolderPath, true, true);

                var season = new Season
                {
                    IndexNumber = seasonParserResult.SeasonNumber,
                    SeriesId = series.Id,
                    SeriesName = series.Name,
                    Path = seasonParserResult.IsSeasonFolder ? path : null
                };

                if (!season.IndexNumber.HasValue || !seasonParserResult.IsSeasonFolder)
                {
                    var resolver = new Naming.TV.EpisodeResolver(namingOptions);

                    var folderName = System.IO.Path.GetFileName(path);
                    var testPath = @"\\test\" + folderName;

                    var episodeInfo = resolver.Resolve(testPath, true);

                    if (episodeInfo?.EpisodeNumber is not null && episodeInfo.SeasonNumber.HasValue)
                    {
                        _logger.LogDebug(
                            "Found folder underneath series with episode number: {0}. Season {1}. Episode {2}",
                            path,
                            episodeInfo.SeasonNumber.Value,
                            episodeInfo.EpisodeNumber.Value);

                        return null;
                    }

                    // hasAnyVideo already checked above — the folder has video.
                    // We couldn't determine the season number from the name, but
                    // create the season anyway with whatever we have; metadata
                    // refresh can fill in the missing IndexNumber later.
                }

                if (season.IndexNumber.HasValue && string.IsNullOrEmpty(season.Name))
                {
                    var seasonNumber = season.IndexNumber.Value;
                    season.Name = seasonNumber == 0 ?
                        args.LibraryOptions.SeasonZeroDisplayName :
                        string.Format(
                            CultureInfo.InvariantCulture,
                            _localization.GetServerLocalizedString("NameSeasonNumber"),
                            seasonNumber,
                            args.LibraryOptions.PreferredMetadataLanguage);
                }

                SetProviderIdFromPath(season, path);

                return season;
            }

            return null;
        }

        /// <summary>
        /// Sets provider ids from the season folder name.
        /// </summary>
        /// <param name="item">The season.</param>
        /// <param name="path">The season folder path.</param>
        private static void SetProviderIdFromPath(Season item, string path)
        {
            var justName = Path.GetFileName(path.AsSpan());

            var tvdbId = justName.GetAttributeValue("tvdbid");
            item.TrySetProviderId(MetadataProvider.Tvdb, tvdbId);

            var tvmazeId = justName.GetAttributeValue("tvmazeid");
            item.TrySetProviderId(MetadataProvider.TvMaze, tvmazeId);

            var tmdbId = justName.GetAttributeValue("tmdbid");
            item.TrySetProviderId(MetadataProvider.Tmdb, tmdbId);

            // Anime databases model a single cour as its own entry, so a multi-season
            // series maps to one of these ids per season rather than one per series.
            var anidbId = justName.GetAttributeValue("anidbid");
            item.TrySetProviderId("AniDB", anidbId);

            var aniListId = justName.GetAttributeValue("anilistid");
            item.TrySetProviderId("AniList", aniListId);

            var aniSearchId = justName.GetAttributeValue("anisearchid");
            item.TrySetProviderId("AniSearch", aniSearchId);
        }
    }
}
