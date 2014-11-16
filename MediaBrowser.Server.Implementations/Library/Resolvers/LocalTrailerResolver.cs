using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Entities;
using System;
using System.IO;
using System.Linq;

namespace MediaBrowser.Server.Implementations.Library.Resolvers
{
    /// <summary>
    /// Class LocalTrailerResolver
    /// </summary>
    public class LocalTrailerResolver : BaseVideoResolver<Trailer>
    {
        private readonly IFileSystem _fileSystem;

        public LocalTrailerResolver(ILibraryManager libraryManager, IFileSystem fileSystem) : base(libraryManager)
        {
            _fileSystem = fileSystem;
        }

        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>Trailer.</returns>
        protected override Trailer Resolve(ItemResolveArgs args)
        {
            // Trailers are not Children, therefore this can never happen
            if (args.Parent != null)
            {
                return null;
            }

            // If the file is within a trailers folder, see if the VideoResolver returns something
            if (!args.IsDirectory)
            {
                if (string.Equals(Path.GetFileName(Path.GetDirectoryName(args.Path)), BaseItem.TrailerFolderName, StringComparison.OrdinalIgnoreCase))
                {
                    return base.Resolve(args);
                }

                // Support xbmc local trailer convention, but only when looking for local trailers (hence the parent == null check)
                if (args.Parent == null)
                {
                    var nameWithoutExtension = _fileSystem.GetFileNameWithoutExtension(args.Path);
                    var suffix = BaseItem.ExtraSuffixes.First(i => i.Value == ExtraType.Trailer);

                    if (nameWithoutExtension.EndsWith(suffix.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        return base.Resolve(args);
                    }
                }
            }

            return null;
        }
    }
}
