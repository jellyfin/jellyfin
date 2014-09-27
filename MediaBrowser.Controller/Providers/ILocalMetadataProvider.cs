using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers
{
    public interface ILocalMetadataProvider : IMetadataProvider
    {
    }

    public interface ILocalMetadataProvider<TItemType> : IMetadataProvider<TItemType>, ILocalMetadataProvider
         where TItemType : IHasMetadata
    {
        /// <summary>
        /// Gets the metadata.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{MetadataResult{`0}}.</returns>
        Task<LocalMetadataResult<TItemType>> GetMetadata(ItemInfo info, CancellationToken cancellationToken);
    }

    public class ItemInfo
    {
        public string Path { get; set; }

        public bool IsInMixedFolder { get; set; }
    }

    public class LocalMetadataResult<T>
        where T : IHasMetadata
    {
        public bool HasMetadata { get; set; }
        public T Item { get; set; }
        
        public List<LocalImageInfo> Images { get; set; }
        public List<ChapterInfo> Chapters { get; set; }
        public List<UserItemData> UserDataLIst { get; set; }

        public LocalMetadataResult()
        {
            Images = new List<LocalImageInfo>();
            Chapters = new List<ChapterInfo>();
            UserDataLIst = new List<UserItemData>();
        }
    }
}
