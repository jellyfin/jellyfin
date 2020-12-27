#pragma warning disable SA1118 // Parameter should not span multiple lines
using System;
using MediaBrowser.Model.Dlna;

namespace Jellyfin.DlnaProfiles.Profiles
{
    /// <summary>
    /// Defines the <see cref="MediaMonkeyProfile" />.
    /// </summary>
    [System.Xml.Serialization.XmlRoot("Profile")]
    public class MediaMonkeyProfile : DefaultProfile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MediaMonkeyProfile"/> class.
        /// </summary>
        public MediaMonkeyProfile()
        {
            Name = "MediaMonkey";

            SupportedMediaTypes = "Audio";

            Identification = new DeviceIdentification(
                @"MediaMonkey",
                new[]
                {
                   new HttpHeaderInfo
                   {
                       Name = "User-Agent",
                       Value = "MediaMonkey",
                       Match = HeaderMatchType.Substring
                   }
                });

            DirectPlayProfiles = new[]
            {
                new DirectPlayProfile("aac,mp3,mpa,wav,wma,mp2,ogg,oga,webma,ape,opus,flac,m4a", null)
            };

            ResponseProfiles = Array.Empty<ResponseProfile>();
        }
    }
}
