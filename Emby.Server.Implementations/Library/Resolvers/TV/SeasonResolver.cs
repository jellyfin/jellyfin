using System.Globalization;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Globalization;
using Emby.Naming.Common;
using Emby.Naming.TV;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Library.Resolvers.TV
{
    /// <summary>
    /// Class SeasonResolver
    /// </summary>
    public class SeasonResolver : FolderResolver<Season>
    {
        /// <summary>
        /// The _config
        /// </summary>
        private readonly IServerConfigurationManager _config;

        private readonly ILibraryManager _libraryManager;
        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");
        private readonly ILocalizationManager _localization;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SeasonResolver"/> class.
        /// </summary>
        /// <param name="config">The config.</param>
        public SeasonResolver(IServerConfigurationManager config, ILibraryManager libraryManager, ILocalizationManager localization, ILogger logger)
        {
            _config = config;
            _libraryManager = libraryManager;
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
            if (args.Parent is Series && args.IsDirectory)
            {
                var namingOptions = ((LibraryManager)_libraryManager).GetNamingOptions();
                var series = ((Series)args.Parent);

                var path = args.Path;

                var seasonParserResult = new SeasonPathParser(namingOptions).Parse(path, true, true);

                var season = new Season
                {
                    IndexNumber = seasonParserResult.SeasonNumber,
                    SeriesId = series.Id,
                    SeriesName = series.Name
                };

                if (!season.IndexNumber.HasValue || !seasonParserResult.IsSeasonFolder)
                {
                    var resolver = new Emby.Naming.TV.EpisodeResolver(namingOptions);

                    var folderName = System.IO.Path.GetFileName(path);
                    var testPath = "\\\\test\\" + folderName;

                    var episodeInfo = resolver.Resolve(testPath, true);

                    if (episodeInfo != null)
                    {
                        if (episodeInfo.EpisodeNumber.HasValue && episodeInfo.SeasonNumber.HasValue)
                        {
                            _logger.LogDebug("Found folder underneath series with episode number: {0}. Season {1}. Episode {2}",
                                path,
                                episodeInfo.SeasonNumber.Value,
                                episodeInfo.EpisodeNumber.Value);

                            return null;
                        }
                    }
                }

                if (season.IndexNumber.HasValue)
                {
                    var seasonNumber = season.IndexNumber.Value;

                    season.Name = seasonNumber == 0 ?
                        args.LibraryOptions.SeasonZeroDisplayName :
                        string.Format(_localization.GetLocalizedString("NameSeasonNumber"), seasonNumber.ToString(UsCulture), args.GetLibraryOptions().PreferredMetadataLanguage);

                }

                return season;
            }

            return null;
        }
    }
}
