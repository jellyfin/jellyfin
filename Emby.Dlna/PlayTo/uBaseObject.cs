#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using Jellyfin.Data.Enums;

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

                if (classType.Contains("Audio", StringComparison.Ordinal))
                {
                    return "Audio";
                }

                if (classType.Contains("Video", StringComparison.Ordinal))
                {
                    return "Video";
                }

                if (classType.Contains("image", StringComparison.Ordinal))
                {
                    return "Photo";
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
