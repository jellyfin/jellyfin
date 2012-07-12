using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Movies.Entities;
using MediaBrowser.Movies.Metadata;

namespace MediaBrowser.Movies.Resolvers
{
    public class MovieResolver : BaseVideoResolver<Movie>
    {
        protected override Movie Resolve(ItemResolveEventArgs args)
        {
            if (args.IsFolder)
            {
                var metadataFile = args.GetFileByName("movie.xml");

                if (metadataFile.HasValue || Path.GetFileName(args.Path).IndexOf("[tmdbid=", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    return GetMovie(args);
                }
            }

            return null;
        }

        private Movie GetMovie(ItemResolveEventArgs args)
        {
            foreach (var child in args.FileSystemChildren)
            {
                ItemResolveEventArgs childArgs = new ItemResolveEventArgs()
                {
                    Path = child.Key,
                    FileAttributes = child.Value,
                    FileSystemChildren = new KeyValuePair<string, FileAttributes>[] { }
                };

                var item = base.Resolve(childArgs);

                if (item != null)
                {
                    return new Movie()
                    {
                        Path = item.Path,
                        VideoType = item.VideoType
                    };
                }
            }

            return new Movie();
        }

        private void PopulateBonusFeatures(Movie item, ItemResolveEventArgs args)
        {
            var trailerPath = args.GetFolderByName("specials");

            if (trailerPath.HasValue)
            {
                string[] allFiles = Directory.GetFileSystemEntries(trailerPath.Value.Key, "*", SearchOption.TopDirectoryOnly);

                item.SpecialFeatures = allFiles.Select(f => Kernel.Instance.ItemController.GetItem(f)).OfType<Video>();
            }
        }

        protected override void SetItemValues(Movie item, ItemResolveEventArgs args)
        {
            base.SetItemValues(item, args);

            var metadataFile = args.GetFileByName("movie.xml");

            if (metadataFile.HasValue)
            {
                new MovieXmlParser().Fetch(item, metadataFile.Value.Key);
            }

            PopulateBonusFeatures(item, args);
        }
    }
}
