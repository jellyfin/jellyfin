#nullable disable

#pragma warning disable CS1591

using System;

namespace MediaBrowser.Controller.Library
{
    public class IntroInfo
    {
        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the item id.
        /// </summary>
        /// <value>The item id.</value>
        public Guid? ItemId { get; set; }
    }
}
