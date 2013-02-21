using System;
using ProtoBuf;
using System.Collections.Generic;

namespace MediaBrowser.Model.Updates
{
    /// <summary>
    /// Class PackageInfo
    /// </summary>
    [ProtoContract]
    public class PackageInfo
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ProtoMember(1)]
        public string name { get; set; }

        /// <summary>
        /// Gets or sets the short description.
        /// </summary>
        /// <value>The short description.</value>
        [ProtoMember(2)]
        public string shortDescription { get; set; }

        /// <summary>
        /// Gets or sets the overview.
        /// </summary>
        /// <value>The overview.</value>
        [ProtoMember(3)]
        public string overview { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is premium.
        /// </summary>
        /// <value><c>true</c> if this instance is premium; otherwise, <c>false</c>.</value>
        [ProtoMember(4)]
        public bool isPremium { get; set; }

        /// <summary>
        /// Gets or sets the rich desc URL.
        /// </summary>
        /// <value>The rich desc URL.</value>
        [ProtoMember(5)]
        public string richDescUrl { get; set; }

        /// <summary>
        /// Gets or sets the thumb image.
        /// </summary>
        /// <value>The thumb image.</value>
        [ProtoMember(6)]
        public string thumbImage { get; set; }

        /// <summary>
        /// Gets or sets the preview image.
        /// </summary>
        /// <value>The preview image.</value>
        [ProtoMember(7)]
        public string previewImage { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        [ProtoMember(8)]
        public PackageType type { get; set; }

        /// <summary>
        /// Gets or sets the target filename.
        /// </summary>
        /// <value>The target filename.</value>
        [ProtoMember(9)]
        public string targetFilename { get; set; }

        /// <summary>
        /// Gets or sets the owner.
        /// </summary>
        /// <value>The owner.</value>
        [ProtoMember(10)]
        public string owner { get; set; }

        /// <summary>
        /// Gets or sets the category.
        /// </summary>
        /// <value>The category.</value>
        [ProtoMember(11)]
        public string category { get; set; }

        /// <summary>
        /// Gets or sets the catalog tile color.
        /// </summary>
        /// <value>The owner.</value>
        [ProtoMember(12)]
        public string tileColor { get; set; }

        /// <summary>
        /// Gets or sets the feature id of this package (if premium).
        /// </summary>
        /// <value>The feature id.</value>
        [ProtoMember(13)]
        public string featureId { get; set; }

        /// <summary>
        /// Gets or sets the registration info for this package (if premium).
        /// </summary>
        /// <value>The registration info.</value>
        [ProtoMember(14)]
        public string regInfo { get; set; }

        /// <summary>
        /// Gets or sets the price for this package (if premium).
        /// </summary>
        /// <value>The price.</value>
        [ProtoMember(15)]
        public float price { get; set; }

        /// <summary>
        /// Gets or sets whether or not this package is registered.
        /// </summary>
        /// <value>True if registered.</value>
        [ProtoMember(16)]
        public bool isRegistered { get; set; }

        /// <summary>
        /// Gets or sets the expiration date for this package.
        /// </summary>
        /// <value>Expiration Date.</value>
        [ProtoMember(17)]
        public DateTime expDate { get; set; }

        /// <summary>
        /// Gets or sets the versions.
        /// </summary>
        /// <value>The versions.</value>
        [ProtoMember(18)]
        public List<PackageVersionInfo> versions { get; set; }
    }
}
