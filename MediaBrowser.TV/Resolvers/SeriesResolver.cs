using System;
using System.ComponentModel.Composition;
using System.IO;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Resolvers;
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

                if (args.ContainsFile("series.xml") || Path.GetFileName(args.Path).IndexOf("[tvdbid=", StringComparison.OrdinalIgnoreCase) != -1 || TVUtils.IsSeriesFolder(args.Path, args.FileSystemChildren))
                {
                    return new Series();
                }
            }

            return null;
        }
    }
}
