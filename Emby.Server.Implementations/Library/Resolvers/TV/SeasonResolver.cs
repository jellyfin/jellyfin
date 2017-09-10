using System.Globalization;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Globalization;
using Emby.Naming.Common;
using Emby.Naming.TV;

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

        /// <summary>
        /// Initializes a new instance of the <see cref="SeasonResolver"/> class.
        /// </summary>
        /// <param name="config">The config.</param>
        public SeasonResolver(IServerConfigurationManager config, ILibraryManager libraryManager, ILocalizationManager localization)
        {
            _config = config;
            _libraryManager = libraryManager;
            _localization = localization;
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

                var season = new Season
                {
                    IndexNumber = new SeasonPathParser(namingOptions, new RegexProvider()).Parse(args.Path, true, true).SeasonNumber,
                    SeriesId = series.Id,
                    SeriesName = series.Name
                };

                if (season.IndexNumber.HasValue)
                {
                    var seasonNumber = season.IndexNumber.Value;

                    season.Name = seasonNumber == 0 ?
                        _config.Configuration.SeasonZeroDisplayName :
                        string.Format(_localization.GetLocalizedString("NameSeasonNumber"), seasonNumber.ToString(UsCulture));
                }

                return season;
            }

            return null;
        }
    }
}
