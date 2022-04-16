#nullable disable

using System;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Net;

namespace MediaBrowser.Controller.Library
{
    /// <summary>
    /// An event that occurs when an item is downloaded.
    /// </summary>
    public class ItemDownloadEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the Item that was downloaded.
        /// </summary>
        public BaseItem Item { get; set; }

        /// <summary>
        /// Gets or sets the authorization info used to download the file.
        /// </summary>
        public AuthorizationInfo AuthInfo { get; set; }
    }
}
