using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using System;
using System.ComponentModel.Composition;

namespace MediaBrowser.Controller.Resolvers.TV
{
    [Export(typeof(IBaseItemResolver))]
    public class SeasonResolver : BaseFolderResolver<Season>
    {
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

        protected override void SetInitialItemValues(Season item, ItemResolveArgs args)
        {
            base.SetInitialItemValues(item, args);

            var series = args.Parent as Series;
            item.SeriesItemId = series != null ? series.Id : Guid.Empty;

            Season.AddMetadataFiles(args);
        }
    }
}
