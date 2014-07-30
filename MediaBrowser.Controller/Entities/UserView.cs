using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Controller.Entities
{
    public class UserView : Folder
    {
        public string ViewType { get; set; }

        public override IEnumerable<BaseItem> GetChildren(User user, bool includeLinkedChildren)
        {
            var mediaFolders = GetMediaFolders(user);

            switch (ViewType)
            {
                case CollectionType.Games:
                    return mediaFolders.SelectMany(i => i.GetRecursiveChildren(user, includeLinkedChildren))
                        .OfType<GameSystem>();
                case CollectionType.BoxSets:
                    return mediaFolders.SelectMany(i => i.GetRecursiveChildren(user, includeLinkedChildren))
                        .OfType<BoxSet>();
                case CollectionType.TvShows:
                    return mediaFolders.SelectMany(i => i.GetRecursiveChildren(user, includeLinkedChildren))
                        .OfType<Series>();
                case CollectionType.Trailers:
                    return mediaFolders.SelectMany(i => i.GetRecursiveChildren(user, includeLinkedChildren))
                        .OfType<Trailer>();
                case CollectionType.Movies:
                    return mediaFolders.SelectMany(i => i.GetRecursiveChildren(user, includeLinkedChildren))
                        .Where(i => i is Movie || i is BoxSet)
                        .DistinctBy(i => i.Id);
                default:
                    return mediaFolders.SelectMany(i => i.GetChildren(user, includeLinkedChildren));
            }
        }

        protected override IEnumerable<BaseItem> GetEligibleChildrenForRecursiveChildren(User user)
        {
            return GetChildren(user, false);
        }

        private IEnumerable<Folder> GetMediaFolders(User user)
        {
            var excludeFolderIds = user.Configuration.ExcludeFoldersFromGrouping.Select(i => new Guid(i)).ToList();

            return user.RootFolder
                .GetChildren(user, true, true)
                .OfType<Folder>()
                .Where(i => !excludeFolderIds.Contains(i.Id) && !IsExcludedFromGrouping(i));
        }

        public static bool IsExcludedFromGrouping(Folder folder)
        {
            var standaloneTypes = new List<string>
            {
                CollectionType.AdultVideos,
                CollectionType.Books,
                CollectionType.HomeVideos,
                CollectionType.Photos,
                CollectionType.Trailers
            };

            var collectionFolder = folder as CollectionFolder;

            if (collectionFolder == null)
            {
                return false;
            }

            return standaloneTypes.Contains(collectionFolder.CollectionType ?? string.Empty);
        }
    }

    public class SpecialFolder : Folder
    {
        public SpecialFolderType SpecialFolderType { get; set; }
        public string ItemTypeName { get; set; }
        public string ParentId { get; set; }

        public override IEnumerable<BaseItem> GetChildren(User user, bool includeLinkedChildren)
        {
            var parent = (Folder)LibraryManager.GetItemById(new Guid(ParentId));

            if (SpecialFolderType == SpecialFolderType.ItemsByType)
            {
                var items = parent.GetRecursiveChildren(user, includeLinkedChildren);

                return items.Where(i => string.Equals(i.GetType().Name, ItemTypeName, StringComparison.OrdinalIgnoreCase));
            }

            return new List<BaseItem>();
        }
    }

    public enum SpecialFolderType
    {
        ItemsByType = 1
    }
}
