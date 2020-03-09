using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Channels
{
    public interface IChannel
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        string Description { get; }

        /// <summary>
        /// Gets the data version.
        /// </summary>
        /// <value>The data version.</value>
        string DataVersion { get; }

        /// <summary>
        /// Gets the home page URL.
        /// </summary>
        /// <value>The home page URL.</value>
        string HomePageUrl { get; }

        /// <summary>
        /// Gets the parental rating.
        /// </summary>
        /// <value>The parental rating.</value>
        ChannelParentalRating ParentalRating { get; }

        /// <summary>
        /// Gets the channel information.
        /// </summary>
        /// <returns>ChannelFeatures.</returns>
        InternalChannelFeatures GetChannelFeatures();

        /// <summary>
        /// Determines whether [is enabled for] [the specified user].
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns><c>true</c> if [is enabled for] [the specified user]; otherwise, <c>false</c>.</returns>
        bool IsEnabledFor(string userId);

        /// <summary>
        /// Gets the channel items.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{ChannelItem}}.</returns>
        Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the channel image.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{DynamicImageResponse}.</returns>
        Task<DynamicImageResponse> GetChannelImage(ImageType type, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the supported channel images.
        /// </summary>
        /// <returns>IEnumerable{ImageType}.</returns>
        IEnumerable<ImageType> GetSupportedChannelImages();
    }
}
