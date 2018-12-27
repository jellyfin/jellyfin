using System;

namespace MediaBrowser.Controller.Library
{
    [Flags]
    public enum ItemUpdateType
    {
        None = 1,
        MetadataImport = 2,
        ImageUpdate = 4,
        MetadataDownload = 8,
        MetadataEdit = 16
    }
}
