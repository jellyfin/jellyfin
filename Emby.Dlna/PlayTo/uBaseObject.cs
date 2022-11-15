#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;

namespace Emby.Dlna.PlayTo
{
    public class UBaseObject
    {
        public string Id { get; set; }

        public string ParentId { get; set; }

        public string Title { get; set; }

        public string SecondText { get; set; }

        public string IconUrl { get; set; }

        public string MetaData { get; set; }

        public string Url { get; set; }

        public IReadOnlyList<string> ProtocolInfo { get; set; }

        public string UpnpClass { get; set; }

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

        public bool Equals(UBaseObject obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            return string.Equals(Id, obj.Id, StringComparison.Ordinal);
        }
    }
}
