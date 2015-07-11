using MediaBrowser.Controller.Entities;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Providers
{
    public class LocalMetadataResult<T> : MetadataResult<T>
        where T : IHasMetadata
    {
        public List<LocalImageInfo> Images { get; set; }
        public List<UserItemData> UserDataLIst { get; set; }

        public LocalMetadataResult()
        {
            Images = new List<LocalImageInfo>();
            UserDataLIst = new List<UserItemData>();
        }
    }
}