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
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediaBrowser.Movies.Resolvers
{
    [Export(typeof(IBaseItemResolver))]
    public class MovieResolver : BaseVideoResolver<Movie>
    {
        protected override Movie Resolve(ItemResolveEventArgs args)
        {
            if ((args.VirtualFolderCollectionType ?? string.Empty).Equals("Movies", StringComparison.OrdinalIgnoreCase) && args.IsDirectory)
            {
                if (args.ContainsFile("movie.xml") || Path.GetFileName(args.Path).IndexOf("[tmdbid=", StringComparison.OrdinalIgnoreCase) != -1)
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
                    FileInfo = child,
                    FileSystemChildren = new WIN32_FIND_DATA[] { },
                    Path = child.Path
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

        /*private void PopulateBonusFeatures(Movie item, ItemResolveEventArgs args)
        {
            if (args.ContainsFolder("specials"))
            {
                List<Video> items = new List<Video>();

                foreach (WIN32_FIND_DATA file in FileData.GetFileSystemEntries(Path.Combine(args.Path, "specials"), "*"))
                {
                    Video video = await Kernel.Instance.ItemController.GetItem(file.Path, fileInfo: file).ConfigureAwait(false) as Video;

                    if (video != null)
                    {
                        items.Add(video);
                    }
                }

                (item as BaseItem).LocalTrailers = items;
            }
        }*/
    }
}
