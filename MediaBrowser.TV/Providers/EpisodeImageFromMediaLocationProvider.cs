using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.TV.Entities;

namespace MediaBrowser.TV.Providers
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

        public override Task Fetch(BaseEntity item, ItemResolveEventArgs args)
        {
            return Task.Run(() =>
            {
                Episode episode = item as Episode;

                string metadataFolder = Path.Combine(args.Parent.Path, "metadata");

                string episodeFileName = Path.GetFileName(episode.Path);

                Season season = args.Parent as Season;

                SetPrimaryImagePath(episode, season, metadataFolder, episodeFileName);
            });
        }

        private void SetPrimaryImagePath(Episode item, Season season, string metadataFolder, string episodeFileName)
        {
            string[] imageFiles = new string[] {
                Path.Combine(metadataFolder, Path.ChangeExtension(episodeFileName, ".jpg")),
                Path.Combine(metadataFolder, Path.ChangeExtension(episodeFileName, ".png"))
            };

            string image;

            if (season == null)
            {
                // Gotta do this the slow way
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
