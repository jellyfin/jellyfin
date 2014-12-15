using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;

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

        /// <summary>
        /// Initializes a new instance of the <see cref="SeasonResolver"/> class.
        /// </summary>
        /// <param name="config">The config.</param>
        public SeasonResolver(IServerConfigurationManager config)
        {
            _config = config;
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
                var season = new Season
                {
                    IndexNumber = SeriesResolver.GetSeasonNumberFromPath(args.Path, CollectionType.TvShows)
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
