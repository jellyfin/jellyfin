#pragma warning disable SA1118 // Parameter should not span multiple lines
using MediaBrowser.Model.Dlna;

namespace Jellyfin.DlnaProfiles.Profiles
{
    /// <summary>
    /// Defines the <see cref="MarantzProfile" />.
    /// </summary>
    [System.Xml.Serialization.XmlRoot("Profile")]
    public class MarantzProfile : DefaultProfile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MarantzProfile"/> class.
        /// </summary>
        public MarantzProfile()
        {
            Name = "Marantz";

            SupportedMediaTypes = "Audio";

            Identification = new DeviceIdentification(
                null,
                new[]
                {
                    new HttpHeaderInfo
                    {
                        Name = "User-Agent",
                        Value = "Marantz",
                        Match = HeaderMatchType.Substring
                    }
                })
            {
                Manufacturer = @"Marantz"
            };

            DirectPlayProfiles = new[]
            {
                new DirectPlayProfile("aac,mp3,wav,wma,flac", null)
            };

            ResponseProfiles = System.Array.Empty<ResponseProfile>();
        }
    }
}
