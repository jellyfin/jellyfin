#pragma warning disable SA1118 // Parameter should not span multiple lines

using MediaBrowser.Model.Dlna;

namespace Jellyfin.DlnaProfiles.Profiles
{
    /// <summary>
    /// Defines the <see cref="Foobar2000Profile" />.
    /// </summary>
    [System.Xml.Serialization.XmlRoot("Profile")]
    public class Foobar2000Profile : DefaultProfile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Foobar2000Profile"/> class.
        /// </summary>
        public Foobar2000Profile()
        {
            Name = "foobar2000";

            SupportedMediaTypes = "Audio";

            Identification = new DeviceIdentification(
                @"foobar",
                new[]
                {
                   new HttpHeaderInfo
                   {
                       Name = "User-Agent",
                       Value = "foobar",
                       Match = HeaderMatchType.Substring
                   }
                });

            DirectPlayProfiles = new[]
            {
                new DirectPlayProfile("mp3", "mp2,mp3"),
                new DirectPlayProfile("mp4", "mp4"),
                new DirectPlayProfile("aac,wav", null),
                new DirectPlayProfile("flac", "flac"),
                new DirectPlayProfile("asf", "wmav2,wmapro,wmavoice"),
                new DirectPlayProfile("ogg", "vorbis")
            };

            ResponseProfiles = System.Array.Empty<ResponseProfile>();
        }
    }
}
