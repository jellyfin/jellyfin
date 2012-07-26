using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.TV.Entities;
using MediaBrowser.TV.Metadata;

namespace MediaBrowser.TV.Resolvers
{
    [Export(typeof(IBaseItemResolver))]
    public class EpisodeResolver : BaseVideoResolver<Episode>
    {
        protected override Episode Resolve(ItemResolveEventArgs args)
        {
            if (args.Parent is Season || args.Parent is Series)
            {
                return base.Resolve(args);
            }

            return null;
        }

        protected override void SetItemValues(Episode item, ItemResolveEventArgs args)
        {
            base.SetItemValues(item, args);

            string metadataFolder = Path.Combine(args.Parent.Path, "metadata");

            string episodeFileName = Path.GetFileName(item.Path);

            string metadataFile = Path.Combine(metadataFolder, Path.ChangeExtension(episodeFileName, ".xml"));

            Season season = args.Parent as Season;

            FetchMetadata(item, season, metadataFile);

            if (string.IsNullOrEmpty(item.PrimaryImagePath))
            {
                SetPrimaryImagePath(item, season, metadataFolder, episodeFileName);
            }
        }

        private void FetchMetadata(Episode item, Season season, string metadataFile)
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

            new EpisodeXmlParser().Fetch(item, metadataFile);
        }

        private void SetPrimaryImagePath(Episode item, Season season, string metadataFolder, string episodeFileName)
        {
            string[] imageFiles = new string[] {
                Path.Combine(metadataFolder, Path.ChangeExtension(episodeFileName, ".jpg")),
                Path.Combine(metadataFolder, Path.ChangeExtension(episodeFileName, ".png"))
            };

            if (season == null)
            {
                // Gotta do this the slow way
                item.PrimaryImagePath = imageFiles.FirstOrDefault(f => File.Exists(f));
            }
            else
            {
                item.PrimaryImagePath = imageFiles.FirstOrDefault(f => season.MetadataFiles.Any(s => s.Equals(f, StringComparison.OrdinalIgnoreCase)));
            }
        }
    }
}
