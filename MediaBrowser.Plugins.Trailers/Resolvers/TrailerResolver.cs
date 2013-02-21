using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;
using System;
using System.ComponentModel.Composition;
using System.Linq;

namespace MediaBrowser.Plugins.Trailers.Resolvers
{
    /// <summary>
    /// Class TrailerResolver
    /// </summary>
    [Export(typeof(IBaseItemResolver))]
    public class TrailerResolver : BaseVideoResolver<Trailer>
    {
        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>Trailer.</returns>
        protected override Trailer Resolve(ItemResolveArgs args)
        {
            // Must be a directory and under the trailer download folder
            if (args.IsDirectory && args.Path.StartsWith(Plugin.Instance.DownloadPath, StringComparison.OrdinalIgnoreCase))
            {
                // The trailer must be a video file
                return FindTrailer(args);
            }

            return null;
        }

        /// <summary>
        /// Finds a movie based on a child file system entries
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>Trailer.</returns>
        private Trailer FindTrailer(ItemResolveArgs args)
        {
            // Loop through each child file/folder and see if we find a video
            return args.FileSystemChildren
                .Where(c => !c.IsDirectory)
                .Select(child => base.Resolve(new ItemResolveArgs
                {
                    FileInfo = child,
                    Path = child.Path
                }))
                .FirstOrDefault(i => i != null);
        }
    }
}
