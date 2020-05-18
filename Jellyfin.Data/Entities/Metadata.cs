using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities
{
    public abstract partial class Metadata
    {
        partial void Init();

        /// <summary>
        /// Default constructor. Protected due to being abstract.
        /// </summary>
        protected Metadata()
        {
            PersonRoles = new HashSet<PersonRole>();
            Genres = new HashSet<Genre>();
            Artwork = new HashSet<Artwork>();
            Ratings = new HashSet<Rating>();
            Sources = new HashSet<MetadataProviderId>();

            Init();
        }

        /// <summary>
        /// Public constructor with required data
        /// </summary>
        /// <param name="title">The title or name of the object</param>
        /// <param name="language">ISO-639-3 3-character language codes</param>
        protected Metadata(string title, string language, DateTime dateadded, DateTime datemodified)
        {
            if (string.IsNullOrEmpty(title)) throw new ArgumentNullException(nameof(title));
            this.Title = title;

            if (string.IsNullOrEmpty(language)) throw new ArgumentNullException(nameof(language));
            this.Language = language;

            this.PersonRoles = new HashSet<PersonRole>();
            this.Genres = new HashSet<Genre>();
            this.Artwork = new HashSet<Artwork>();
            this.Ratings = new HashSet<Rating>();
            this.Sources = new HashSet<MetadataProviderId>();

            Init();
        }

        /*************************************************************************
         * Properties
         *************************************************************************/

        /// <summary>
        /// Backing field for Id
        /// </summary>
        internal int _Id;
        /// <summary>
        /// When provided in a partial class, allows value of Id to be changed before setting.
        /// </summary>
        partial void SetId(int oldValue, ref int newValue);
        /// <summary>
        /// When provided in a partial class, allows value of Id to be changed before returning.
        /// </summary>
        partial void GetId(ref int result);

        /// <summary>
        /// Identity, Indexed, Required
        /// </summary>
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id
        {
            get
            {
                int value = _Id;
                GetId(ref value);
                return (_Id = value);
            }
            protected set
            {
                int oldValue = _Id;
                SetId(oldValue, ref value);
                if (oldValue != value)
                {
                    _Id = value;
                }
            }
        }

        /// <summary>
        /// Backing field for Title
        /// </summary>
        protected string _Title;
        /// <summary>
        /// When provided in a partial class, allows value of Title to be changed before setting.
        /// </summary>
        partial void SetTitle(string oldValue, ref string newValue);
        /// <summary>
        /// When provided in a partial class, allows value of Title to be changed before returning.
        /// </summary>
        partial void GetTitle(ref string result);

        /// <summary>
        /// Required, Max length = 1024
        /// The title or name of the object
        /// </summary>
        [Required]
        [MaxLength(1024)]
        [StringLength(1024)]
        public string Title
        {
            get
            {
                string value = _Title;
                GetTitle(ref value);
                return (_Title = value);
            }
            set
            {
                string oldValue = _Title;
                SetTitle(oldValue, ref value);
                if (oldValue != value)
                {
                    _Title = value;
                }
            }
        }

        /// <summary>
        /// Backing field for OriginalTitle
        /// </summary>
        protected string _OriginalTitle;
        /// <summary>
        /// When provided in a partial class, allows value of OriginalTitle to be changed before setting.
        /// </summary>
        partial void SetOriginalTitle(string oldValue, ref string newValue);
        /// <summary>
        /// When provided in a partial class, allows value of OriginalTitle to be changed before returning.
        /// </summary>
        partial void GetOriginalTitle(ref string result);

        /// <summary>
        /// Max length = 1024
        /// </summary>
        [MaxLength(1024)]
        [StringLength(1024)]
        public string OriginalTitle
        {
            get
            {
                string value = _OriginalTitle;
                GetOriginalTitle(ref value);
                return (_OriginalTitle = value);
            }
            set
            {
                string oldValue = _OriginalTitle;
                SetOriginalTitle(oldValue, ref value);
                if (oldValue != value)
                {
                    _OriginalTitle = value;
                }
            }
        }

        /// <summary>
        /// Backing field for SortTitle
        /// </summary>
        protected string _SortTitle;
        /// <summary>
        /// When provided in a partial class, allows value of SortTitle to be changed before setting.
        /// </summary>
        partial void SetSortTitle(string oldValue, ref string newValue);
        /// <summary>
        /// When provided in a partial class, allows value of SortTitle to be changed before returning.
        /// </summary>
        partial void GetSortTitle(ref string result);

        /// <summary>
        /// Max length = 1024
        /// </summary>
        [MaxLength(1024)]
        [StringLength(1024)]
        public string SortTitle
        {
            get
            {
                string value = _SortTitle;
                GetSortTitle(ref value);
                return (_SortTitle = value);
            }
            set
            {
                string oldValue = _SortTitle;
                SetSortTitle(oldValue, ref value);
                if (oldValue != value)
                {
                    _SortTitle = value;
                }
            }
        }

        /// <summary>
        /// Backing field for Language
        /// </summary>
        protected string _Language;
        /// <summary>
        /// When provided in a partial class, allows value of Language to be changed before setting.
        /// </summary>
        partial void SetLanguage(string oldValue, ref string newValue);
        /// <summary>
        /// When provided in a partial class, allows value of Language to be changed before returning.
        /// </summary>
        partial void GetLanguage(ref string result);

        /// <summary>
        /// Required, Min length = 3, Max length = 3
        /// ISO-639-3 3-character language codes
        /// </summary>
        [Required]
        [MinLength(3)]
        [MaxLength(3)]
        [StringLength(3)]
        public string Language
        {
            get
            {
                string value = _Language;
                GetLanguage(ref value);
                return (_Language = value);
            }
            set
            {
                string oldValue = _Language;
                SetLanguage(oldValue, ref value);
                if (oldValue != value)
                {
                    _Language = value;
                }
            }
        }

        /// <summary>
        /// Backing field for ReleaseDate
        /// </summary>
        protected DateTimeOffset? _ReleaseDate;
        /// <summary>
        /// When provided in a partial class, allows value of ReleaseDate to be changed before setting.
        /// </summary>
        partial void SetReleaseDate(DateTimeOffset? oldValue, ref DateTimeOffset? newValue);
        /// <summary>
        /// When provided in a partial class, allows value of ReleaseDate to be changed before returning.
        /// </summary>
        partial void GetReleaseDate(ref DateTimeOffset? result);

        public DateTimeOffset? ReleaseDate
        {
            get
            {
                DateTimeOffset? value = _ReleaseDate;
                GetReleaseDate(ref value);
                return (_ReleaseDate = value);
            }
            set
            {
                DateTimeOffset? oldValue = _ReleaseDate;
                SetReleaseDate(oldValue, ref value);
                if (oldValue != value)
                {
                    _ReleaseDate = value;
                }
            }
        }

        /// <summary>
        /// Backing field for DateAdded
        /// </summary>
        protected DateTime _DateAdded;
        /// <summary>
        /// When provided in a partial class, allows value of DateAdded to be changed before setting.
        /// </summary>
        partial void SetDateAdded(DateTime oldValue, ref DateTime newValue);
        /// <summary>
        /// When provided in a partial class, allows value of DateAdded to be changed before returning.
        /// </summary>
        partial void GetDateAdded(ref DateTime result);

        /// <summary>
        /// Required
        /// </summary>
        [Required]
        public DateTime DateAdded
        {
            get
            {
                DateTime value = _DateAdded;
                GetDateAdded(ref value);
                return (_DateAdded = value);
            }
            internal set
            {
                DateTime oldValue = _DateAdded;
                SetDateAdded(oldValue, ref value);
                if (oldValue != value)
                {
                    _DateAdded = value;
                }
            }
        }

        /// <summary>
        /// Backing field for DateModified
        /// </summary>
        protected DateTime _DateModified;
        /// <summary>
        /// When provided in a partial class, allows value of DateModified to be changed before setting.
        /// </summary>
        partial void SetDateModified(DateTime oldValue, ref DateTime newValue);
        /// <summary>
        /// When provided in a partial class, allows value of DateModified to be changed before returning.
        /// </summary>
        partial void GetDateModified(ref DateTime result);

        /// <summary>
        /// Required
        /// </summary>
        [Required]
        public DateTime DateModified
        {
            get
            {
                DateTime value = _DateModified;
                GetDateModified(ref value);
                return (_DateModified = value);
            }
            internal set
            {
                DateTime oldValue = _DateModified;
                SetDateModified(oldValue, ref value);
                if (oldValue != value)
                {
                    _DateModified = value;
                }
            }
        }

        /// <summary>
        /// Required, ConcurrenyToken
        /// </summary>
        [ConcurrencyCheck]
        [Required]
        public uint RowVersion { get; set; }

        public void OnSavingChanges()
        {
            RowVersion++;
        }

        /*************************************************************************
         * Navigation properties
         *************************************************************************/

        [ForeignKey("PersonRole_PersonRoles_Id")]
        public virtual ICollection<PersonRole> PersonRoles { get; protected set; }

        [ForeignKey("PersonRole_PersonRoles_Id")]
        public virtual ICollection<Genre> Genres { get; protected set; }

        [ForeignKey("PersonRole_PersonRoles_Id")]
        public virtual ICollection<Artwork> Artwork { get; protected set; }

        [ForeignKey("PersonRole_PersonRoles_Id")]
        public virtual ICollection<Rating> Ratings { get; protected set; }

        [ForeignKey("PersonRole_PersonRoles_Id")]
        public virtual ICollection<MetadataProviderId> Sources { get; protected set; }

    }
}

