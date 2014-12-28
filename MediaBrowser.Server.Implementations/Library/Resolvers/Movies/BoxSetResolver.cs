using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using System;
using System.IO;
using System.Linq;

namespace MediaBrowser.Server.Implementations.Library.Resolvers.Movies
{
    /// <summary>
    /// Class BoxSetResolver
    /// </summary>
    public class BoxSetResolver : FolderResolver<BoxSet>
    {
        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>BoxSet.</returns>
        protected override BoxSet Resolve(ItemResolveArgs args)
        {
            // It's a boxset if all of the following conditions are met:
            // Is a Directory
            // Contains [boxset] in the path
            if (args.IsDirectory)
            {
                if (IsInvalid(args.GetCollectionType()))
                {
                    return null;
                }
                
                var filename = Path.GetFileName(args.Path);

                if (string.IsNullOrEmpty(filename))
                {
                    return null;
                }
                
                if (filename.IndexOf("[boxset]", StringComparison.OrdinalIgnoreCase) != -1 || 
                    args.ContainsFileSystemEntryByName("collection.xml"))
                {
                    return new BoxSet
                    {
                        Path = args.Path,
                        Name = ResolverHelper.StripBrackets(Path.GetFileName(args.Path))
                    };
                }
            }

            return null;
        }

        private bool IsInvalid(string collectionType)
        {
            var validCollectionTypes = new[]
            {
                CollectionType.Movies,
                CollectionType.BoxSets
            };

            return !validCollectionTypes.Contains(collectionType ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Sets the initial item values.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="args">The args.</param>
        protected override void SetInitialItemValues(BoxSet item, ItemResolveArgs args)
        {
            base.SetInitialItemValues(item, args);

            SetProviderIdFromPath(item);
        }

        /// <summary>
        /// Sets the provider id from path.
        /// </summary>
        /// <param name="item">The item.</param>
        private void SetProviderIdFromPath(BaseItem item)
        {
            //we need to only look at the name of this actual item (not parents)
            var justName = Path.GetFileName(item.Path);

            var id = justName.GetAttributeValue("tmdbid");

            if (!string.IsNullOrEmpty(id))
            {
                item.SetProviderId(MetadataProviders.Tmdb, id);
            }
        }
    }
}
