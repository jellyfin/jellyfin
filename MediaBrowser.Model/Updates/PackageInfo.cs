using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.Updates
{
    /// <summary>
    /// Class PackageInfo
    /// </summary>
    public class PackageInfo
    {
        /// <summary>
        /// The internal id of this package.
        /// </summary>
        /// <value>The id.</value>
        public string id { get; set; }

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
        /// Gets or sets a value indicating whether this instance is premium.
        /// </summary>
        /// <value><c>true</c> if this instance is premium; otherwise, <c>false</c>.</value>
        public bool isPremium { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is adult only content.
        /// </summary>
        /// <value><c>true</c> if this instance is adult; otherwise, <c>false</c>.</value>
        public bool adult { get; set; }

        /// <summary>
        /// Gets or sets the rich desc URL.
        /// </summary>
        /// <value>The rich desc URL.</value>
        public string richDescUrl { get; set; }

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
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        public string type { get; set; }

        /// <summary>
        /// Gets or sets the target filename.
        /// </summary>
        /// <value>The target filename.</value>
        public string targetFilename { get; set; }

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
        /// Gets or sets the catalog tile color.
        /// </summary>
        /// <value>The owner.</value>
        public string tileColor { get; set; }

        /// <summary>
        /// Gets or sets the feature id of this package (if premium).
        /// </summary>
        /// <value>The feature id.</value>
        public string featureId { get; set; }

        /// <summary>
        /// Gets or sets the registration info for this package (if premium).
        /// </summary>
        /// <value>The registration info.</value>
        public string regInfo { get; set; }

        /// <summary>
        /// Gets or sets the price for this package (if premium).
        /// </summary>
        /// <value>The price.</value>
        public float price { get; set; }

        /// <summary>
        /// Gets or sets the target system for this plug-in (Server, MBTheater, MBClassic).
        /// </summary>
        /// <value>The target system.</value>
        public PackageTargetSystem targetSystem { get; set; }

        /// <summary>
        /// The guid of the assembly associated with this package (if a plug-in).
        /// This is used to identify the proper item for automatic updates.
        /// </summary>
        /// <value>The name.</value>
        public string guid { get; set; }

        /// <summary>
        /// Gets or sets the total number of ratings for this package.
        /// </summary>
        /// <value>The total ratings.</value>
        public int? totalRatings { get; set; }

        /// <summary>
        /// Gets or sets the average rating for this package .
        /// </summary>
        /// <value>The rating.</value>
        public float avgRating { get; set; }

        /// <summary>
        /// Gets or sets whether or not this package is registered.
        /// </summary>
        /// <value>True if registered.</value>
        public bool isRegistered { get; set; }

        /// <summary>
        /// Gets or sets the expiration date for this package.
        /// </summary>
        /// <value>Expiration Date.</value>
        public DateTime expDate { get; set; }

        /// <summary>
        /// Gets or sets the versions.
        /// </summary>
        /// <value>The versions.</value>
        public List<PackageVersionInfo> versions { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [enable in application store].
        /// </summary>
        /// <value><c>true</c> if [enable in application store]; otherwise, <c>false</c>.</value>
        public bool enableInAppStore { get; set; }

        /// <summary>
        /// Gets or sets the installs.
        /// </summary>
        /// <value>The installs.</value>
        public int installs { get; set; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="PackageInfo"/> class.
        /// </summary>
        public PackageInfo()
        {
            versions = new List<PackageVersionInfo>();
        }
    }
}
