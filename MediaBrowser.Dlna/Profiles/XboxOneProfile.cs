using MediaBrowser.Controller.Dlna;

namespace MediaBrowser.Dlna.Profiles
{
    public class XboxOneProfile : DefaultProfile
    {
        public XboxOneProfile()
        {
            Name = "Xbox One";

            Identification = new DeviceIdentification
            {
                ModelName = "Xbox One",
                FriendlyName = "Xbox-SystemOS"
            };

            TranscodingProfiles = new[]
            {
                new TranscodingProfile
                {
                    Container = "mp3",
                    AudioCodec = "mp3",
                    Type = DlnaProfileType.Audio
                },
                new TranscodingProfile
                {
                    Container = "ts",
                    VideoCodec = "h264",
                    AudioCodec = "aac",
                    Type = DlnaProfileType.Video
                }
            };

            DirectPlayProfiles = new[]
            {
                new DirectPlayProfile
                {
                    Container = "mp3",
                    Type = DlnaProfileType.Audio
                }
            };

            MediaProfiles = new[]
            {
                new MediaProfile
                {
                    Container = "avi",
                    MimeType = "video/x-msvideo",
                    Type = DlnaProfileType.Video
                }
            };
        }
    }
}
