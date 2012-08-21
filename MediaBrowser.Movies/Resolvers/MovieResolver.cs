using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Movies.Entities;

namespace MediaBrowser.Movies.Resolvers
{
    [Export(typeof(IBaseItemResolver))]
    public class MovieResolver : BaseVideoResolver<Movie>
    {
        protected override Movie Resolve(ItemResolveEventArgs args)
        {
            if (args.IsFolder && (args.VirtualFolderCollectionType ?? string.Empty).Equals("Movies", StringComparison.OrdinalIgnoreCase))
            {
                var metadataFile = args.GetFileSystemEntryByName("movie.xml");

                if (metadataFile.HasValue || Path.GetFileName(args.Path).IndexOf("[tmdbid=", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    return GetMovie(args) ?? new Movie();
                }

                // If it's not a boxset, the only other allowed parent type is Folder
                if (!(args.Parent is BoxSet))
                {
                    if (args.Parent != null && args.Parent.GetType() != typeof(Folder))
                    {
                        return null;
                    }
                }

                // There's no metadata or [tmdb in the path, now we will have to work some magic to see if this is a Movie
                if (args.Parent != null)
                {
                    return GetMovie(args);
                }
            }

            return null;
        }

        private Movie GetMovie(ItemResolveEventArgs args)
        {
            for (var i = 0; i < args.FileSystemChildren.Length; i++)
            {
                var child = args.FileSystemChildren[i];

                ItemResolveEventArgs childArgs = new ItemResolveEventArgs()
                {
                    Path = child.Path,
                    FileData = child.FileInfo,
                    FileSystemChildren = new LazyFileInfo[] { }
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

            return null;
        }

        private void PopulateBonusFeatures(Movie item, ItemResolveEventArgs args)
        {
            var trailerPath = args.GetFileSystemEntryByName("specials", true);

            if (trailerPath.HasValue)
            {
                string[] allFiles = Directory.GetFileSystemEntries(trailerPath.Value.Path, "*", SearchOption.TopDirectoryOnly);

                item.SpecialFeatures = allFiles.Select(f => Kernel.Instance.ItemController.GetItem(f)).OfType<Video>();
            }
        }

        protected override void SetInitialItemValues(Movie item, ItemResolveEventArgs args)
        {
            base.SetInitialItemValues(item, args);

            PopulateBonusFeatures(item, args);
        }
    }
}
