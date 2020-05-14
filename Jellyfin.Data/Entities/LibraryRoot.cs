using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities
{
    public partial class LibraryRoot
    {
        partial void Init();

        /// <summary>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </summary>
        protected LibraryRoot()
        {
            Init();
        }

        /// <summary>
        /// Replaces default constructor, since it's protected. Caller assumes responsibility for setting all required values before saving.
        /// </summary>
        public static LibraryRoot CreateLibraryRootUnsafe()
        {
            return new LibraryRoot();
        }

        /// <summary>
        /// Public constructor with required data
        /// </summary>
        /// <param name="path">Absolute Path</param>
        public LibraryRoot(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            this.Path = path;


            Init();
        }

        /// <summary>
        /// Static create function (for use in LINQ queries, etc.)
        /// </summary>
        /// <param name="path">Absolute Path</param>
        public static LibraryRoot Create(string path)
        {
            return new LibraryRoot(path);
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
        /// Absolute Path
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
        /// Backing field for NetworkPath
        /// </summary>
        protected string _NetworkPath;
        /// <summary>
        /// When provided in a partial class, allows value of NetworkPath to be changed before setting.
        /// </summary>
        partial void SetNetworkPath(string oldValue, ref string newValue);
        /// <summary>
        /// When provided in a partial class, allows value of NetworkPath to be changed before returning.
        /// </summary>
        partial void GetNetworkPath(ref string result);

        /// <summary>
        /// Max length = 65535
        /// Absolute network path, for example for transcoding sattelites.
        /// </summary>
        [MaxLength(65535)]
        [StringLength(65535)]
        public string NetworkPath
        {
            get
            {
                string value = _NetworkPath;
                GetNetworkPath(ref value);
                return (_NetworkPath = value);
            }
            set
            {
                string oldValue = _NetworkPath;
                SetNetworkPath(oldValue, ref value);
                if (oldValue != value)
                {
                    _NetworkPath = value;
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

        /// <summary>
        /// Required
        /// </summary>
        [ForeignKey("Library_Id")]
        public virtual Library Library { get; set; }

    }
}

