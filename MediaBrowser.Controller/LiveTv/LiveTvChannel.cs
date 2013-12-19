using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.LiveTv;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaBrowser.Controller.LiveTv
{
    public class LiveTvChannel : BaseItem, IItemByName
    {
        public LiveTvChannel()
        {
            UserItemCountList = new List<ItemByNameCounts>();
        }

        /// <summary>
        /// Gets the user data key.
        /// </summary>
        /// <returns>System.String.</returns>
        public override string GetUserDataKey()
        {
            return GetClientTypeName() + "-" + Name;
        }

        [IgnoreDataMember]
        public List<ItemByNameCounts> UserItemCountList { get; set; }

        public ChannelInfo ChannelInfo { get; set; }

        public string ServiceName { get; set; }

        protected override string CreateSortName()
        {
            double number = 0;

            if (!string.IsNullOrEmpty(ChannelInfo.Number))
            {
                double.TryParse(ChannelInfo.Number, out number);
            }

            return number.ToString("000-") + (Name ?? string.Empty);
        }

        public override string MediaType
        {
            get
            {
                return ChannelInfo.ChannelType == ChannelType.Radio ? Model.Entities.MediaType.Audio : Model.Entities.MediaType.Video;
            }
        }

        public override string GetClientTypeName()
        {
            return "Channel";
        }
    }
}
