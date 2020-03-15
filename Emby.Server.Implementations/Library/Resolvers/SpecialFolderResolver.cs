#pragma warning disable CS1591

using System;
using System.IO;
using System.Linq;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.IO;

namespace Emby.Server.Implementations.Library.Resolvers
{
    public class SpecialFolderResolver : FolderResolver<Folder>
    {
        private readonly IFileSystem _fileSystem;
        private readonly IServerApplicationPaths _appPaths;

        public SpecialFolderResolver(IFileSystem fileSystem, IServerApplicationPaths appPaths)
        {
            _fileSystem = fileSystem;
            _appPaths = appPaths;
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override ResolverPriority Priority => ResolverPriority.First;

        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>Folder.</returns>
        protected override Folder Resolve(ItemResolveArgs args)
        {
            if (args.IsDirectory)
            {
                if (args.IsPhysicalRoot)
                {
                    return new AggregateFolder();
                }
                if (string.Equals(args.Path, _appPaths.DefaultUserViewsPath, StringComparison.OrdinalIgnoreCase))
                {
                    return new UserRootFolder();  //if we got here and still a root - must be user root
                }
                if (args.IsVf)
                {
                    return new CollectionFolder
                    {
                        CollectionType = GetCollectionType(args),
                        PhysicalLocationsList = args.PhysicalLocations
                    };
                }
            }

            return null;
        }

        private string GetCollectionType(ItemResolveArgs args)
        {
            return args.FileSystemChildren
                .Where(i =>
                {

                    try
                    {
                        return !i.IsDirectory &&
                               string.Equals(".collection", i.Extension, StringComparison.OrdinalIgnoreCase);
                    }
                    catch (IOException)
                    {
                        return false;
                    }

                })
                .Select(i => _fileSystem.GetFileNameWithoutExtension(i))
                .FirstOrDefault();
        }
    }
}
