#pragma warning disable CS1591
#pragma warning disable CS1819

using System;

namespace Emby.Dlna.PlayTo
{
    public class uBaseObject
    {
        public string Id { get; set; } = string.Empty;

        public string ParentId { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string SecondText { get; set; } = string.Empty;

        public string IconUrl { get; set; } = string.Empty;

        public string MetaData { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        public string[] ProtocolInfo { get; set; } = Array.Empty<string>();

        public string UpnpClass { get; set; } = string.Empty;

        public bool Equals(uBaseObject item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            return string.Equals(Id, item.Id, StringComparison.OrdinalIgnoreCase);
        }

        public string? MediaType
        {
            get
            {
                var classType = UpnpClass ?? string.Empty;

                if (classType.IndexOf(MediaBrowser.Model.Entities.MediaType.Audio, StringComparison.Ordinal) != -1)
                {
                    return MediaBrowser.Model.Entities.MediaType.Audio;
                }

                if (classType.IndexOf(MediaBrowser.Model.Entities.MediaType.Video, StringComparison.Ordinal) != -1)
                {
                    return MediaBrowser.Model.Entities.MediaType.Video;
                }

                if (classType.IndexOf("image", StringComparison.Ordinal) != -1)
                {
                    return MediaBrowser.Model.Entities.MediaType.Photo;
                }

                return null;
            }
        }
    }
}
