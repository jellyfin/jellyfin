using MediaBrowser.Model.Dlna;

namespace Jellyfin.DlnaProfiles.Profiles
{
    /// <summary>
    /// Defines the <see cref="DenonAvrProfile" />.
    /// </summary>
    [System.Xml.Serialization.XmlRoot("Profile")]
    public class DenonAvrProfile : DefaultProfile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DenonAvrProfile"/> class.
        /// </summary>
        public DenonAvrProfile()
        {
            Name = "Denon AVR";

            SupportedMediaTypes = "Audio";

            Identification = new DeviceIdentification(@"Denon:\[AVR:.*")
            {
                Manufacturer = "Denon"
            };

            DirectPlayProfiles = new[]
            {
                new DirectPlayProfile("mp3,flac,m4a,wma", null)
                {
                    Type = DlnaProfileType.Audio
                },
            };

            CodecProfiles = new[]
            {
                new CodecProfile(
                    null,
                    CodecType.Audio,
                    new[]
                    {
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.AudioSampleRate, "96000", true)
                    })
                {
                    Container = "flac"
                }
            };

            ResponseProfiles = System.Array.Empty<ResponseProfile>();
        }
    }
}
