using System;
using System.ComponentModel.Composition;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
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
            // Must be a directory and under a 'Movies' VF
            if ((args.VirtualFolderCollectionType ?? string.Empty).Equals("Movies", StringComparison.OrdinalIgnoreCase) && args.IsDirectory)
            {
                // Return a movie if the video resolver finds something in the folder
                return GetMovie(args);
            }

            return null;
        }

        protected override void SetInitialItemValues(Movie item, ItemResolveEventArgs args)
        {
            base.SetInitialItemValues(item, args);

            SetProviderIdFromPath(item);
        }

        private void SetProviderIdFromPath(Movie item)
        {
            string srch = "[tmdbid=";
            int index = item.Path.IndexOf(srch, System.StringComparison.OrdinalIgnoreCase);

            if (index != -1)
            {
                string id = item.Path.Substring(index + srch.Length);

                id = id.Substring(0, id.IndexOf(']'));

                item.SetProviderId(MetadataProviders.Tmdb, id);
            }
        }

        private Movie GetMovie(ItemResolveEventArgs args)
        {
            // Loop through each child file/folder and see if we find a video
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
