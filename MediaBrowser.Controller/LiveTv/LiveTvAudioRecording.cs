using System.Runtime.Serialization;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Users;
using System.Linq;

namespace MediaBrowser.Controller.LiveTv
{
    public class LiveTvAudioRecording : Audio, ILiveTvRecording
    {
        /// <summary>
        /// Gets the user data key.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string CreateUserDataKey()
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

        /// <summary>
        /// Gets a value indicating whether this instance is owned item.
        /// </summary>
        /// <value><c>true</c> if this instance is owned item; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public override bool IsOwnedItem
        {
            get
            {
                return false;
            }
        }

        [IgnoreDataMember]
        public override string MediaType
        {
            get
            {
                return Model.Entities.MediaType.Audio;
            }
        }

        [IgnoreDataMember]
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

        [IgnoreDataMember]
        public override bool SupportsLocalMetadata
        {
            get
            {
                return false;
            }
        }

        protected override bool GetBlockUnratedValue(UserPolicy config)
        {
            return config.BlockUnratedItems.Contains(UnratedItem.LiveTvProgram);
        }

        protected override string GetInternalMetadataPath(string basePath)
        {
            return System.IO.Path.Combine(basePath, "livetv", Id.ToString("N"));
        }
    }
}
