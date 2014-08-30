using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.LiveTv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Entities
{
    public class UserView : Folder
    {
        public string ViewType { get; set; }
        public static IUserViewManager UserViewManager { get; set; }

        public override IEnumerable<BaseItem> GetChildren(User user, bool includeLinkedChildren)
        {
            var mediaFolders = GetMediaFolders(user);

            switch (ViewType)
            {
                case CollectionType.LiveTvChannels:
                    return LiveTvManager.GetInternalChannels(new LiveTvChannelQuery
                    {
                        UserId = user.Id.ToString("N")

                    }, CancellationToken.None).Result.Items;
                case CollectionType.LiveTvRecordingGroups:
                    return LiveTvManager.GetInternalRecordings(new RecordingQuery
                    {
                        UserId = user.Id.ToString("N"),
                        Status = RecordingStatus.Completed

                    }, CancellationToken.None).Result.Items;
                case CollectionType.LiveTv:
                    return GetLiveTvFolders(user).Result;
                case CollectionType.Folders:
                    return user.RootFolder.GetChildren(user, includeLinkedChildren);
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
                default:
                    return mediaFolders.SelectMany(i => i.GetChildren(user, includeLinkedChildren));
            }
        }

        private async Task<IEnumerable<BaseItem>> GetLiveTvFolders(User user)
        {
            var list = new List<BaseItem>();

            list.Add(await UserViewManager.GetUserView(CollectionType.LiveTvChannels, user, string.Empty, CancellationToken.None).ConfigureAwait(false));
            list.Add(await UserViewManager.GetUserView(CollectionType.LiveTvRecordingGroups, user, string.Empty, CancellationToken.None).ConfigureAwait(false));

            return list;
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
