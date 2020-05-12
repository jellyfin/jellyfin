using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities
{
    public partial class Release
    {
        partial void Init();

        /// <summary>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </summary>
        protected Release()
        {
            MediaFiles = new HashSet<MediaFile>();
            Chapters = new HashSet<Chapter>();

            Init();
        }

        /// <summary>
        /// Replaces default constructor, since it's protected. Caller assumes responsibility for setting all required values before saving.
        /// </summary>
        public static Release CreateReleaseUnsafe()
        {
            return new Release();
        }

        /// <summary>
        /// Public constructor with required data
        /// </summary>
        /// <param name="name"></param>
        /// <param name="_movie0"></param>
        /// <param name="_episode1"></param>
        /// <param name="_track2"></param>
        /// <param name="_customitem3"></param>
        /// <param name="_book4"></param>
        /// <param name="_photo5"></param>
        public Release(string name, Movie _movie0, Episode _episode1, Track _track2, CustomItem _customitem3, Book _book4, Photo _photo5)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            this.Name = name;

            if (_movie0 == null) throw new ArgumentNullException(nameof(_movie0));
            _movie0.Releases.Add(this);

            if (_episode1 == null) throw new ArgumentNullException(nameof(_episode1));
            _episode1.Releases.Add(this);

            if (_track2 == null) throw new ArgumentNullException(nameof(_track2));
            _track2.Releases.Add(this);

            if (_customitem3 == null) throw new ArgumentNullException(nameof(_customitem3));
            _customitem3.Releases.Add(this);

            if (_book4 == null) throw new ArgumentNullException(nameof(_book4));
            _book4.Releases.Add(this);

            if (_photo5 == null) throw new ArgumentNullException(nameof(_photo5));
            _photo5.Releases.Add(this);

            this.MediaFiles = new HashSet<MediaFile>();
            this.Chapters = new HashSet<Chapter>();

            Init();
        }

        /// <summary>
        /// Static create function (for use in LINQ queries, etc.)
        /// </summary>
        /// <param name="name"></param>
        /// <param name="_movie0"></param>
        /// <param name="_episode1"></param>
        /// <param name="_track2"></param>
        /// <param name="_customitem3"></param>
        /// <param name="_book4"></param>
        /// <param name="_photo5"></param>
        public static Release Create(string name, Movie _movie0, Episode _episode1, Track _track2, CustomItem _customitem3, Book _book4, Photo _photo5)
        {
            return new Release(name, _movie0, _episode1, _track2, _customitem3, _book4, _photo5);
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
        /// Backing field for Name
        /// </summary>
        protected string _Name;
        /// <summary>
        /// When provided in a partial class, allows value of Name to be changed before setting.
        /// </summary>
        partial void SetName(string oldValue, ref string newValue);
        /// <summary>
        /// When provided in a partial class, allows value of Name to be changed before returning.
        /// </summary>
        partial void GetName(ref string result);

        /// <summary>
        /// Required, Max length = 1024
        /// </summary>
        [Required]
        [MaxLength(1024)]
        [StringLength(1024)]
        public string Name
        {
            get
            {
                string value = _Name;
                GetName(ref value);
                return (_Name = value);
            }
            set
            {
                string oldValue = _Name;
                SetName(oldValue, ref value);
                if (oldValue != value)
                {
                    _Name = value;
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
        [ForeignKey("MediaFile_MediaFiles_Id")]
        public virtual ICollection<MediaFile> MediaFiles { get; protected set; }

        [ForeignKey("Chapter_Chapters_Id")]
        public virtual ICollection<Chapter> Chapters { get; protected set; }

    }
}

