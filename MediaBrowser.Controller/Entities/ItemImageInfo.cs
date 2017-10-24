using MediaBrowser.Model.Entities;
using System;
using MediaBrowser.Model.Serialization;

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

        public int Width { get; set; }
        public int Height { get; set; }

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
