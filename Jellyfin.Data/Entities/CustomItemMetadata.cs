using System;

namespace Jellyfin.Data.Entities
{
    public partial class CustomItemMetadata : Metadata
    {
        partial void Init();

        /// <summary>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </summary>
        protected CustomItemMetadata()
        {
            Init();
        }

        /// <summary>
        /// Replaces default constructor, since it's protected. Caller assumes responsibility for setting all required values before saving.
        /// </summary>
        public static CustomItemMetadata CreateCustomItemMetadataUnsafe()
        {
            return new CustomItemMetadata();
        }

        /// <summary>
        /// Public constructor with required data
        /// </summary>
        /// <param name="title">The title or name of the object</param>
        /// <param name="language">ISO-639-3 3-character language codes</param>
        /// <param name="_customitem0"></param>
        public CustomItemMetadata(string title, string language, DateTime dateadded, DateTime datemodified, CustomItem _customitem0)
        {
            if (string.IsNullOrEmpty(title)) throw new ArgumentNullException(nameof(title));
            this.Title = title;

            if (string.IsNullOrEmpty(language)) throw new ArgumentNullException(nameof(language));
            this.Language = language;

            if (_customitem0 == null) throw new ArgumentNullException(nameof(_customitem0));
            _customitem0.CustomItemMetadata.Add(this);


            Init();
        }

        /// <summary>
        /// Static create function (for use in LINQ queries, etc.)
        /// </summary>
        /// <param name="title">The title or name of the object</param>
        /// <param name="language">ISO-639-3 3-character language codes</param>
        /// <param name="_customitem0"></param>
        public static CustomItemMetadata Create(string title, string language, DateTime dateadded, DateTime datemodified, CustomItem _customitem0)
        {
            return new CustomItemMetadata(title, language, dateadded, datemodified, _customitem0);
        }

        /*************************************************************************
         * Properties
         *************************************************************************/

        /*************************************************************************
         * Navigation properties
         *************************************************************************/

    }
}

