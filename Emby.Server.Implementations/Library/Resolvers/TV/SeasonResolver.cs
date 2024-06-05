#nullable disable

using System.Globalization;
using Emby.Naming.Common;
using Emby.Naming.TV;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
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

                var seasonParserResult = SeasonPathParser.Parse(path, true, true);

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
                }

                if (season.IndexNumber.HasValue && string.IsNullOrEmpty(season.Name))
                {
                    var seasonNumber = season.IndexNumber.Value;
                    season.Name = seasonNumber == 0 ?
                        args.LibraryOptions.SeasonZeroDisplayName :
                        string.Format(
                            CultureInfo.InvariantCulture,
                            _localization.GetLocalizedString("NameSeasonNumber"),
                            seasonNumber,
                            args.LibraryOptions.PreferredMetadataLanguage);
                }

                return season;
            }

            return null;
        }
    }
}
