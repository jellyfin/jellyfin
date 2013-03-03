using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using System;

namespace MediaBrowser.Server.Implementations.Library.Resolvers.TV
{
    /// <summary>
    /// Class SeasonResolver
    /// </summary>
    public class SeasonResolver : FolderResolver<Season>
    {
        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>Season.</returns>
        protected override Season Resolve(ItemResolveArgs args)
        {
            if (args.Parent is Series && args.IsDirectory)
            {
                return new Season
                {
                    IndexNumber = TVUtils.GetSeasonNumberFromPath(args.Path)
                };
            }

            return null;
        }

        /// <summary>
        /// Sets the initial item values.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="args">The args.</param>
        protected override void SetInitialItemValues(Season item, ItemResolveArgs args)
        {
            base.SetInitialItemValues(item, args);

            var series = args.Parent as Series;
            item.SeriesItemId = series != null ? series.Id : Guid.Empty;

            Season.AddMetadataFiles(args);
        }
    }
}
