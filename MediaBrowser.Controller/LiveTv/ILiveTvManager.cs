using MediaBrowser.Model.LiveTv;
using System.Collections.Generic;

namespace MediaBrowser.Controller.LiveTv
{
    /// <summary>
    /// Manages all live tv services installed on the server
    /// </summary>
    public interface ILiveTvManager
    {
        /// <summary>
        /// Gets the services.
        /// </summary>
        /// <value>The services.</value>
        IReadOnlyList<ILiveTvService> Services { get; }

        /// <summary>
        /// Adds the parts.
        /// </summary>
        /// <param name="services">The services.</param>
        void AddParts(IEnumerable<ILiveTvService> services);

        /// <summary>
        /// Gets the channels.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>IEnumerable{Channel}.</returns>
        IEnumerable<Channel> GetChannels(ChannelQuery query);

        /// <summary>
        /// Gets the channel information dto.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <returns>ChannelInfoDto.</returns>
        ChannelInfoDto GetChannelInfoDto(Channel info);

        /// <summary>
        /// Gets the channel.
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        /// <param name="channelId">The channel identifier.</param>
        /// <returns>Channel.</returns>
        Channel GetChannel(string serviceName, string channelId);
    }
}
