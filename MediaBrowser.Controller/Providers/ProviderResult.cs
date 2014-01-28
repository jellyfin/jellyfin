using System;

namespace MediaBrowser.Controller.Providers
{
    public class ProviderResult
    {
        /// <summary>
        /// Gets or sets the item identifier.
        /// </summary>
        /// <value>The item identifier.</value>
        public Guid ItemId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance has refreshed metadata.
        /// </summary>
        /// <value><c>true</c> if this instance has refreshed metadata; otherwise, <c>false</c>.</value>
        public bool HasRefreshedMetadata { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance has refreshed images.
        /// </summary>
        /// <value><c>true</c> if this instance has refreshed images; otherwise, <c>false</c>.</value>
        public bool HasRefreshedImages { get; set; }

        /// <summary>
        /// Gets or sets the date last refreshed.
        /// </summary>
        /// <value>The date last refreshed.</value>
        public DateTime DateLastRefreshed { get; set; }

        /// <summary>
        /// Gets or sets the last result.
        /// </summary>
        /// <value>The last result.</value>
        public ProviderRefreshStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the last result error message.
        /// </summary>
        /// <value>The last result error message.</value>
        public string ErrorMessage { get; set; }

        public void AddStatus(ProviderRefreshStatus status, string errorMessage)
        {
            if (string.IsNullOrEmpty(ErrorMessage))
            {
                ErrorMessage = errorMessage;
            }
            if (Status == ProviderRefreshStatus.Success)
            {
                Status = status;
            }
        }

        public ProviderResult()
        {
            Status = ProviderRefreshStatus.Success;
        }
    }
}
