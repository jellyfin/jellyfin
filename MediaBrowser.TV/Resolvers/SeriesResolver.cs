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
            if (args.IsFolder && (args.VirtualFolderCollectionType ?? string.Empty).Equals("TV", StringComparison.OrdinalIgnoreCase))
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

            // Read data from series.xml, if it exists
            PopulateFolderMetadata(item, args);
        }

        private void PopulateFolderMetadata(Series item, ItemResolveEventArgs args)
        {
            var metadataFile = args.GetFileByName("series.xml");

            if (metadataFile.HasValue)
            {
                new SeriesXmlParser().Fetch(item, metadataFile.Value.Key);
            }
        }
    }
}
