using System.ComponentModel.Composition;
using System.IO;
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

        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.First; }
        }

        public override Task FetchAsync(BaseEntity item, ItemResolveEventArgs args)
        {
            return Fetch(item, args);
        }

        private Task Fetch(BaseEntity item, ItemResolveEventArgs args)
        {
            string metadataFolder = Path.Combine(args.Parent.Path, "metadata");

            string metadataFile = Path.Combine(metadataFolder, Path.ChangeExtension(Path.GetFileName(args.Path), ".xml"));

            return FetchMetadata(item as Episode, args.Parent as Season, metadataFile);
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
                if (!season.ContainsMetadataFile(metadataFile))
                {
                    return;
                }
            }

            await Task.Run(() => { new EpisodeXmlParser().Fetch(item, metadataFile); }).ConfigureAwait(false);
        }
    }
}
