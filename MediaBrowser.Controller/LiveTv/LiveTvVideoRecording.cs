using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.LiveTv
{
    public class LiveTvVideoRecording : Video, ILiveTvRecording
    {
        /// <summary>
        /// Gets the user data key.
        /// </summary>
        /// <returns>System.String.</returns>
        public override string GetUserDataKey()
        {
            var name = GetClientTypeName();

            if (!string.IsNullOrEmpty(RecordingInfo.ProgramId))
            {
                return name + "-" + RecordingInfo.ProgramId;
            }

            return name + "-" + RecordingInfo.Name + (RecordingInfo.EpisodeTitle ?? string.Empty);
        }

        public RecordingInfo RecordingInfo { get; set; }

        public string ServiceName { get; set; }

        public override string MediaType
        {
            get
            {
                return Model.Entities.MediaType.Video;
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

        /// <summary>
        /// Gets a value indicating whether this instance is owned item.
        /// </summary>
        /// <value><c>true</c> if this instance is owned item; otherwise, <c>false</c>.</value>
        public override bool IsOwnedItem
        {
            get
            {
                return false;
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

        public override bool SupportsLocalMetadata
        {
            get
            {
                return false;
            }
        }
    }
}
