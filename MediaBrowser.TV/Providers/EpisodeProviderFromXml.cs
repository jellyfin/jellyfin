using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.TV.Entities;
using MediaBrowser.TV.Metadata;

namespace MediaBrowser.TV.Providers
{
    [Export(typeof(BaseMetadataProvider))]
    public class EpisodeProviderFromXml : BaseMetadataProvider
    {
        public override bool Supports(BaseEntity item)
        {
            return item is Episode;
        }

        public async override Task Fetch(BaseEntity item, ItemResolveEventArgs args)
        {
            string metadataFolder = Path.Combine(args.Parent.Path, "metadata");

            Episode episode = item as Episode;

            string episodeFileName = Path.GetFileName(episode.Path);

            string metadataFile = Path.Combine(metadataFolder, Path.ChangeExtension(episodeFileName, ".xml"));

            await FetchMetadata(episode, args.Parent as Season, metadataFile);
        }

        private async Task FetchMetadata(Episode item, Season season, string metadataFile)
        {
            if (season == null)
            {
                // Episode directly in Series folder
                // Need to validate it the slow way
                if (!File.Exists(metadataFile))
                {
                    return;
                }
            }
            else
            {
                if (!season.MetadataFiles.Any(s => s.Equals(metadataFile, StringComparison.OrdinalIgnoreCase)))
                {
                    return;
                }
            }

            await new EpisodeXmlParser().Fetch(item, metadataFile);
        }
    }
}
