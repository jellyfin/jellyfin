using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers.TV
{
    [Export(typeof(BaseMetadataProvider))]
    public class EpisodeImageFromMediaLocationProvider : BaseMetadataProvider
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
            return Task.Run(() =>
            {
                var episode = item as Episode;

                string metadataFolder = Path.Combine(args.Parent.Path, "metadata");

                string episodeFileName = Path.GetFileName(episode.Path);

                var season = args.Parent as Season;

                SetPrimaryImagePath(episode, season, metadataFolder, episodeFileName);
            });
        }

        private void SetPrimaryImagePath(Episode item, Season season, string metadataFolder, string episodeFileName)
        {
            // Look for the image file in the metadata folder, and if found, set PrimaryImagePath
            var imageFiles = new string[] {
                Path.Combine(metadataFolder, Path.ChangeExtension(episodeFileName, ".jpg")),
                Path.Combine(metadataFolder, Path.ChangeExtension(episodeFileName, ".png"))
            };

            string image;

            if (season == null)
            {
                // Epsiode directly in Series folder. Gotta do this the slow way
                image = imageFiles.FirstOrDefault(f => File.Exists(f));
            }
            else
            {
                image = imageFiles.FirstOrDefault(f => season.ContainsMetadataFile(f));
            }

            // If we found something, set PrimaryImagePath
            if (!string.IsNullOrEmpty(image))
            {
                item.PrimaryImagePath = image;
            }
        }
    }
}
