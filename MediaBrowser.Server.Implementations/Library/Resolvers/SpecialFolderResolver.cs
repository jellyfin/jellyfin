using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;
using System;
using System.IO;
using System.Linq;
using CommonIO;

namespace MediaBrowser.Server.Implementations.Library.Resolvers
{
    class SpecialFolderResolver : FolderResolver<Folder>
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
        public override ResolverPriority Priority
        {
            get { return ResolverPriority.First; }
        }

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
                        CollectionType = GetCollectionType(args)
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
                        return (i.Attributes & FileAttributes.Directory) != FileAttributes.Directory &&
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
