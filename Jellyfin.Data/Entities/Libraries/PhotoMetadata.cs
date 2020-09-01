using System;

namespace Jellyfin.Data.Entities.Libraries
{
    /// <summary>
    /// An entity that holds metadata for a photo.
    /// </summary>
    public class PhotoMetadata : ItemMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PhotoMetadata"/> class.
        /// </summary>
        /// <param name="title">The title or name of the photo.</param>
        /// <param name="language">ISO-639-3 3-character language codes.</param>
        /// <param name="photo">The photo.</param>
        public PhotoMetadata(string title, string language, Photo photo) : base(title, language)
        {
            if (photo == null)
            {
                throw new ArgumentNullException(nameof(photo));
            }

            photo.PhotoMetadata.Add(this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PhotoMetadata"/> class.
        /// </summary>
        /// <remarks>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </remarks>
        protected PhotoMetadata()
        {
        }
    }
}
