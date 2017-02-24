using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Controller.Entities.Audio
{
    public class AudioPodcast : Audio
    {
        [IgnoreDataMember]
        public override bool SupportsPositionTicksResume
        {
            get
            {
                return true;
            }
        }

        public override double? GetDefaultPrimaryImageAspectRatio()
        {
            return null;
        }
    }
}
