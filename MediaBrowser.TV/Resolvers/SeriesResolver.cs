using System;
using System.ComponentModel.Composition;
using System.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Entities;
using MediaBrowser.TV.Entities;

namespace MediaBrowser.TV.Resolvers
{
    [Export(typeof(IBaseItemResolver))]
    public class SeriesResolver : BaseFolderResolver<Series>
    {
        protected override Series Resolve(ItemResolveEventArgs args)
        {
            if (args.IsDirectory && (args.VirtualFolderCollectionType ?? string.Empty).Equals("TV", StringComparison.OrdinalIgnoreCase))
            {
                // Optimization to avoid running these tests against Seasons
                if (args.Parent is Series)
                {
                    return null;
                }

                // It's a Series if any of the following conditions are met:
                // series.xml exists
                // [tvdbid= is present in the path
                // TVUtils.IsSeriesFolder returns true
                if (args.ContainsFile("series.xml") || Path.GetFileName(args.Path).IndexOf("[tvdbid=", StringComparison.OrdinalIgnoreCase) != -1 || TVUtils.IsSeriesFolder(args.Path, args.FileSystemChildren))
                {
                    return new Series();
                }
            }

            return null;
        }

        protected override void SetInitialItemValues(Series item, ItemResolveEventArgs args)
        {
            base.SetInitialItemValues(item, args);

            SetProviderIdFromPath(item);
        }

        private void SetProviderIdFromPath(Series item)
        {
            string srch = "[tvdbid=";
            int index = item.Path.IndexOf(srch, System.StringComparison.OrdinalIgnoreCase);

            if (index != -1)
            {
                string id = item.Path.Substring(index + srch.Length);

                id = id.Substring(0, id.IndexOf(']'));

                item.SetProviderId(MetadataProviders.Tvdb, id);
            }
        }
    }
}
