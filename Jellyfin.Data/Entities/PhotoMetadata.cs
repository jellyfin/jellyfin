using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities
{
    public partial class PhotoMetadata : Metadata
    {
        partial void Init();

        /// <summary>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </summary>
        protected PhotoMetadata()
        {
            Init();
        }

        /// <summary>
        /// Replaces default constructor, since it's protected. Caller assumes responsibility for setting all required values before saving.
        /// </summary>
        public static PhotoMetadata CreatePhotoMetadataUnsafe()
        {
            return new PhotoMetadata();
        }

        /// <summary>
        /// Public constructor with required data
        /// </summary>
        /// <param name="title">The title or name of the object</param>
        /// <param name="language">ISO-639-3 3-character language codes</param>
        /// <param name="_photo0"></param>
        public PhotoMetadata(string title, string language, DateTime dateadded, DateTime datemodified, Photo _photo0)
        {
            if (string.IsNullOrEmpty(title)) throw new ArgumentNullException(nameof(title));
            this.Title = title;

            if (string.IsNullOrEmpty(language)) throw new ArgumentNullException(nameof(language));
            this.Language = language;

            if (_photo0 == null) throw new ArgumentNullException(nameof(_photo0));
            _photo0.PhotoMetadata.Add(this);


            Init();
        }

        /// <summary>
        /// Static create function (for use in LINQ queries, etc.)
        /// </summary>
        /// <param name="title">The title or name of the object</param>
        /// <param name="language">ISO-639-3 3-character language codes</param>
        /// <param name="_photo0"></param>
        public static PhotoMetadata Create(string title, string language, DateTime dateadded, DateTime datemodified, Photo _photo0)
        {
            return new PhotoMetadata(title, language, dateadded, datemodified, _photo0);
        }

        /*************************************************************************
         * Properties
         *************************************************************************/

        /*************************************************************************
         * Navigation properties
         *************************************************************************/

    }
}

