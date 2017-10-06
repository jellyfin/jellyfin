using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Controller.Entities.Audio
{
    public class AudioPodcast : Audio, IHasLookupInfo<SongInfo>
    {
        [IgnoreDataMember]
        public override bool SupportsPositionTicksResume
        {
            get
            {
                return true;
            }
        }

        [IgnoreDataMember]
        public override bool SupportsPlayedStatus
        {
            get
            {
                return true;
            }
        }

        public override double? GetDefaultPrimaryImageAspectRatio()
        {
            return 1;
        }
    }
}
