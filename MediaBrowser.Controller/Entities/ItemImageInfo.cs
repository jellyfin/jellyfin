using MediaBrowser.Model.Entities;
using System;
using System.Runtime.Serialization;

namespace MediaBrowser.Controller.Entities
{
    public class ItemImageInfo
    {
        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        public ImageType Type { get; set; }

        /// <summary>
        /// Gets or sets the date modified.
        /// </summary>
        /// <value>The date modified.</value>
        public DateTime DateModified { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is placeholder.
        /// </summary>
        /// <value><c>true</c> if this instance is placeholder; otherwise, <c>false</c>.</value>
        public bool IsPlaceholder { get; set; }

        [IgnoreDataMember]
        public bool IsLocalFile
        {
            get
            {
                if (Path != null)
                {
                    if (Path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
                return true;
            }
        }
    }
}
