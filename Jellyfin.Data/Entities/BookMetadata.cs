using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities
{
    public partial class BookMetadata : Metadata
    {
        partial void Init();

        /// <summary>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </summary>
        protected BookMetadata()
        {
            Publishers = new HashSet<Company>();

            Init();
        }

        /// <summary>
        /// Replaces default constructor, since it's protected. Caller assumes responsibility for setting all required values before saving.
        /// </summary>
        public static BookMetadata CreateBookMetadataUnsafe()
        {
            return new BookMetadata();
        }

        /// <summary>
        /// Public constructor with required data
        /// </summary>
        /// <param name="title">The title or name of the object</param>
        /// <param name="language">ISO-639-3 3-character language codes</param>
        /// <param name="_book0"></param>
        public BookMetadata(string title, string language, DateTime dateadded, DateTime datemodified, Book _book0)
        {
            if (string.IsNullOrEmpty(title)) throw new ArgumentNullException(nameof(title));
            this.Title = title;

            if (string.IsNullOrEmpty(language)) throw new ArgumentNullException(nameof(language));
            this.Language = language;

            if (_book0 == null) throw new ArgumentNullException(nameof(_book0));
            _book0.BookMetadata.Add(this);

            this.Publishers = new HashSet<Company>();

            Init();
        }

        /// <summary>
        /// Static create function (for use in LINQ queries, etc.)
        /// </summary>
        /// <param name="title">The title or name of the object</param>
        /// <param name="language">ISO-639-3 3-character language codes</param>
        /// <param name="_book0"></param>
        public static BookMetadata Create(string title, string language, DateTime dateadded, DateTime datemodified, Book _book0)
        {
            return new BookMetadata(title, language, dateadded, datemodified, _book0);
        }

        /*************************************************************************
         * Properties
         *************************************************************************/

        /// <summary>
        /// Backing field for ISBN
        /// </summary>
        protected long? _ISBN;
        /// <summary>
        /// When provided in a partial class, allows value of ISBN to be changed before setting.
        /// </summary>
        partial void SetISBN(long? oldValue, ref long? newValue);
        /// <summary>
        /// When provided in a partial class, allows value of ISBN to be changed before returning.
        /// </summary>
        partial void GetISBN(ref long? result);

        public long? ISBN
        {
            get
            {
                long? value = _ISBN;
                GetISBN(ref value);
                return (_ISBN = value);
            }
            set
            {
                long? oldValue = _ISBN;
                SetISBN(oldValue, ref value);
                if (oldValue != value)
                {
                    _ISBN = value;
                }
            }
        }

        /*************************************************************************
         * Navigation properties
         *************************************************************************/

        [ForeignKey("Company_Publishers_Id")]
        public virtual ICollection<Company> Publishers { get; protected set; }

    }
}

