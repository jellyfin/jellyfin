using System.Threading.Tasks;
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
        /// Gets the channel info dto.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <returns>ChannelInfoDto.</returns>
        ChannelInfoDto GetChannelInfoDto(ChannelInfo info);

        RecordingInfo GetRecordingInfo(RecordingInfo info);
    }
}
