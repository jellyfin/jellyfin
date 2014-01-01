using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.LiveTv
{
    public class LiveTvAudioRecording : Audio, ILiveTvRecording
    {
        /// <summary>
        /// Gets the user data key.
        /// </summary>
        /// <returns>System.String.</returns>
        public override string GetUserDataKey()
        {
            return GetClientTypeName() + "-" + Name;
        }

        public RecordingInfo RecordingInfo { get; set; }

        public string ServiceName { get; set; }

        public override string MediaType
        {
            get
            {
                return Model.Entities.MediaType.Audio;
            }
        }

        public override LocationType LocationType
        {
            get
            {
                if (!string.IsNullOrEmpty(Path))
                {
                    return base.LocationType;
                }

                return LocationType.Remote;
            }
        }

        public override string GetClientTypeName()
        {
            return "Recording";
        }

        public override bool IsSaveLocalMetadataEnabled()
        {
            return false;
        }
    }
}
