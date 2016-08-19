using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Naming.Common;
using MediaBrowser.Naming.TV;

namespace MediaBrowser.Server.Implementations.Library.Resolvers.TV
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

        /// <summary>
        /// Initializes a new instance of the <see cref="SeasonResolver"/> class.
        /// </summary>
        /// <param name="config">The config.</param>
        public SeasonResolver(IServerConfigurationManager config, ILibraryManager libraryManager)
        {
            _config = config;
            _libraryManager = libraryManager;
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
                    SeriesSortName = series.SortName,
                    SeriesName = series.Name
                };

                if (season.IndexNumber.HasValue && season.IndexNumber.Value == 0)
                {
                    season.Name = _config.Configuration.SeasonZeroDisplayName;
                }

                return season;
            }

            return null;
        }
    }
}
