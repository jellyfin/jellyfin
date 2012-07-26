using System;
using System.ComponentModel.Composition;
using System.IO;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.TV.Entities;
using MediaBrowser.TV.Metadata;

namespace MediaBrowser.TV.Resolvers
{
    [Export(typeof(IBaseItemResolver))]
    public class SeriesResolver : BaseFolderResolver<Series>
    {
        protected override Series Resolve(ItemResolveEventArgs args)
        {
            if (args.IsFolder)
            {
                // Optimization to avoid running these tests against VF's
                if (args.Parent != null && args.Parent.IsRoot)
                {
                    return null;
                }

                // Optimization to avoid running these tests against Seasons
                if (args.Parent is Series)
                {
                    return null;
                }

                var metadataFile = args.GetFileByName("series.xml");

                if (metadataFile.HasValue || Path.GetFileName(args.Path).IndexOf("[tvdbid=", StringComparison.OrdinalIgnoreCase) != -1 || TVUtils.IsSeriesFolder(args.Path, args.FileSystemChildren))
                {
                    return new Series();
                }
            }

            return null;
        }

        protected override void SetItemValues(Series item, ItemResolveEventArgs args)
        {
            base.SetItemValues(item, args);

            var metadataFile = args.GetFileByName("series.xml");

            if (metadataFile.HasValue)
            {
                new SeriesXmlParser().Fetch(item, metadataFile.Value.Key);
            }
        }
    }
}
