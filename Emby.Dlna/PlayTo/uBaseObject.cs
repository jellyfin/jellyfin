#pragma warning disable CS1591

using System;

namespace Emby.Dlna.PlayTo
{
    public class uBaseObject
    {
        public string Id { get; set; }

        public string ParentId { get; set; }

        public string Title { get; set; }

        public string SecondText { get; set; }

        public string IconUrl { get; set; }

        public string MetaData { get; set; }

        public string Url { get; set; }

        public string[] ProtocolInfo { get; set; }

        public string UpnpClass { get; set; }

        public bool Equals(uBaseObject obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            return string.Equals(Id, obj.Id);
        }

        public string MediaType
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
