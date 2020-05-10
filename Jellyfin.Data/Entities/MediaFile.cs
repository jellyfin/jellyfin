using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities
{
    public partial class MediaFile
    {
        partial void Init();

        /// <summary>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </summary>
        protected MediaFile()
        {
            MediaFileStreams = new HashSet<MediaFileStream>();

            Init();
        }

        /// <summary>
        /// Replaces default constructor, since it's protected. Caller assumes responsibility for setting all required values before saving.
        /// </summary>
        public static MediaFile CreateMediaFileUnsafe()
        {
            return new MediaFile();
        }

        /// <summary>
        /// Public constructor with required data
        /// </summary>
        /// <param name="path">Relative to the LibraryRoot</param>
        /// <param name="kind"></param>
        /// <param name="_release0"></param>
        public MediaFile(string path, Enums.MediaFileKind kind, Release _release0)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            this.Path = path;

            this.Kind = kind;

            if (_release0 == null) throw new ArgumentNullException(nameof(_release0));
            _release0.MediaFiles.Add(this);

            this.MediaFileStreams = new HashSet<MediaFileStream>();

            Init();
        }

        /// <summary>
        /// Static create function (for use in LINQ queries, etc.)
        /// </summary>
        /// <param name="path">Relative to the LibraryRoot</param>
        /// <param name="kind"></param>
        /// <param name="_release0"></param>
        public static MediaFile Create(string path, Enums.MediaFileKind kind, Release _release0)
        {
            return new MediaFile(path, kind, _release0);
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
        /// Backing field for Path
        /// </summary>
        protected string _Path;
        /// <summary>
        /// When provided in a partial class, allows value of Path to be changed before setting.
        /// </summary>
        partial void SetPath(string oldValue, ref string newValue);
        /// <summary>
        /// When provided in a partial class, allows value of Path to be changed before returning.
        /// </summary>
        partial void GetPath(ref string result);

        /// <summary>
        /// Required, Max length = 65535
        /// Relative to the LibraryRoot
        /// </summary>
        [Required]
        [MaxLength(65535)]
        [StringLength(65535)]
        public string Path
        {
            get
            {
                string value = _Path;
                GetPath(ref value);
                return (_Path = value);
            }
            set
            {
                string oldValue = _Path;
                SetPath(oldValue, ref value);
                if (oldValue != value)
                {
                    _Path = value;
                }
            }
        }

        /// <summary>
        /// Backing field for Kind
        /// </summary>
        protected Enums.MediaFileKind _Kind;
        /// <summary>
        /// When provided in a partial class, allows value of Kind to be changed before setting.
        /// </summary>
        partial void SetKind(Enums.MediaFileKind oldValue, ref Enums.MediaFileKind newValue);
        /// <summary>
        /// When provided in a partial class, allows value of Kind to be changed before returning.
        /// </summary>
        partial void GetKind(ref Enums.MediaFileKind result);

        /// <summary>
        /// Required
        /// </summary>
        [Required]
        public Enums.MediaFileKind Kind
        {
            get
            {
                Enums.MediaFileKind value = _Kind;
                GetKind(ref value);
                return (_Kind = value);
            }
            set
            {
                Enums.MediaFileKind oldValue = _Kind;
                SetKind(oldValue, ref value);
                if (oldValue != value)
                {
                    _Kind = value;
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

        [ForeignKey("MediaFileStream_MediaFileStreams_Id")]
        public virtual ICollection<MediaFileStream> MediaFileStreams { get; protected set; }

    }
}

