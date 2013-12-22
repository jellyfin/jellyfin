using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.LiveTv;

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

        public override string GetClientTypeName()
        {
            return "Program";
        }
    }
}
