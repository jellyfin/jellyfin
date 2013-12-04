using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.LiveTv;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaBrowser.Controller.LiveTv
{
    public class Channel : BaseItem, IItemByName
    {
        public Channel()
        {
            UserItemCountList = new List<ItemByNameCounts>();
        }

        /// <summary>
        /// Gets the user data key.
        /// </summary>
        /// <returns>System.String.</returns>
        public override string GetUserDataKey()
        {
            return "Channel-" + Name;
        }

        [IgnoreDataMember]
        public List<ItemByNameCounts> UserItemCountList { get; set; }

        /// <summary>
        /// Gets or sets the number.
        /// </summary>
        /// <value>The number.</value>
        public string ChannelNumber { get; set; }

        /// <summary>
        /// Get or sets the Id.
        /// </summary>
        /// <value>The id of the channel.</value>
        public string ChannelId { get; set; }

        /// <summary>
        /// Gets or sets the name of the service.
        /// </summary>
        /// <value>The name of the service.</value>
        public string ServiceName { get; set; }

        /// <summary>
        /// Gets or sets the type of the channel.
        /// </summary>
        /// <value>The type of the channel.</value>
        public ChannelType ChannelType { get; set; }

        protected override string CreateSortName()
        {
            double number = 0;

            if (!string.IsNullOrEmpty(ChannelNumber))
            {
                double.TryParse(ChannelNumber, out number);
            }

            return number.ToString("000-") + (Name ?? string.Empty);
        }

        public override string MediaType
        {
            get
            {
                return ChannelType == ChannelType.Radio ? Model.Entities.MediaType.Audio : Model.Entities.MediaType.Video;
            }
        }
    }
}
