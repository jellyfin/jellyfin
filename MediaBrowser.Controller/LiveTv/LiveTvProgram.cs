using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.LiveTv;
using System;

namespace MediaBrowser.Controller.LiveTv
{
    public class LiveTvProgram : BaseItem
    {
        /// <summary>
        /// Gets the user data key.
        /// </summary>
        /// <returns>System.String.</returns>
        public override string GetUserDataKey()
        {
            return GetClientTypeName() + "-" + Name;
        }

        public ProgramInfo ProgramInfo { get; set; }

        public ChannelType ChannelType { get; set; }

        public string ServiceName { get; set; }

        public override string MediaType
        {
            get
            {
                return ChannelType == ChannelType.TV ? Model.Entities.MediaType.Video : Model.Entities.MediaType.Audio;
            }
        }

        public bool IsAiring
        {
            get
            {
                var now = DateTime.UtcNow;

                return now >= ProgramInfo.StartDate && now < ProgramInfo.EndDate;
            }
        }

        public bool HasAired
        {
            get
            {
                var now = DateTime.UtcNow;

                return now >= ProgramInfo.EndDate;
            }
        }

        public override string GetClientTypeName()
        {
            return "Program";
        }
    }
}
