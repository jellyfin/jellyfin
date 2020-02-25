using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.Updates
{
    /// <summary>
    /// Class PackageInfo.
    /// </summary>
    public class PackageInfo
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string name { get; set; }

        /// <summary>
        /// Gets or sets the short description.
        /// </summary>
        /// <value>The short description.</value>
        public string shortDescription { get; set; }

        /// <summary>
        /// Gets or sets the overview.
        /// </summary>
        /// <value>The overview.</value>
        public string overview { get; set; }

        /// <summary>
        /// Gets or sets the thumb image.
        /// </summary>
        /// <value>The thumb image.</value>
        public string thumbImage { get; set; }

        /// <summary>
        /// Gets or sets the preview image.
        /// </summary>
        /// <value>The preview image.</value>
        public string previewImage { get; set; }

        /// <summary>
        /// Gets or sets the target filename for the downloaded binary.
        /// </summary>
        /// <value>The target filename.</value>
        public string filename { get; set; }

        /// <summary>
        /// Gets or sets the owner.
        /// </summary>
        /// <value>The owner.</value>
        public string owner { get; set; }

        /// <summary>
        /// Gets or sets the category.
        /// </summary>
        /// <value>The category.</value>
        public string category { get; set; }

        /// <summary>
        /// The guid of the assembly associated with this plugin.
        /// This is used to identify the proper item for automatic updates.
        /// </summary>
        /// <value>The name.</value>
        public string guid { get; set; }

        /// <summary>
        /// Gets or sets the versions.
        /// </summary>
        /// <value>The versions.</value>
        public IReadOnlyList<VersionInfo> versions { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageInfo"/> class.
        /// </summary>
        public PackageInfo()
        {
            versions = Array.Empty<VersionInfo>();
        }
    }
}
