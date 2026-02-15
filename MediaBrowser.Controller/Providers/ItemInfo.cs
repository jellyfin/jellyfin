#pragma warning disable CS1591

using System;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Providers
{
    public class ItemInfo
    {
        public ItemInfo(BaseItem item)
        {
            Path = item.Path;
            ParentId = item.ParentId;
            IndexNumber = item.IndexNumber;
            ContainingFolderPath = item.ContainingFolderPath;
            IsInMixedFolder = item.IsInMixedFolder;

            if (item is Video video)
            {
                VideoType = video.VideoType;
                IsPlaceHolder = video.IsPlaceHolder;
            }

            ItemType = item.GetType();
        }

        public Type ItemType { get; set; }

        public string Path { get; set; }

        public Guid ParentId { get; set; }

        public int? IndexNumber { get; set; }

        public string ContainingFolderPath { get; set; }

        public VideoType VideoType { get; set; }

        public bool IsInMixedFolder { get; set; }

        public bool IsPlaceHolder { get; set; }
    }
}
