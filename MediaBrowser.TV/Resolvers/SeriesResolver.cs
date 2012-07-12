using System;
using System.IO;
using System.Linq;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.TV.Entities;
using MediaBrowser.TV.Metadata;

namespace MediaBrowser.TV.Resolvers
{
    class SeriesResolver : BaseFolderResolver<Series>
    {
        protected override Series Resolve(ItemResolveEventArgs args)
        {
            if (args.IsFolder)
            {
                var metadataFile = args.GetFileByName("series.xml");

                if (metadataFile.HasValue || Path.GetFileName(args.Path).IndexOf("[tvdbid=", StringComparison.OrdinalIgnoreCase) != -1)
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
