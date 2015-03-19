using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Providers
{
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