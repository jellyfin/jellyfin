using System;
using System.ComponentModel.DataAnnotations;

namespace Jellyfin.Data.Entities
{
    public partial class Artwork
    {
        partial void Init();

        /// <summary>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </summary>
        protected Artwork()
        {
            Init();
        }

        /// <summary>
        /// Replaces default constructor, since it's protected. Caller assumes responsibility for setting all required values before saving.
        /// </summary>
        public static Artwork CreateArtworkUnsafe()
        {
            return new Artwork();
        }

        /// <summary>
        /// Public constructor with required data
        /// </summary>
        /// <param name="path"></param>
        /// <param name="kind"></param>
        /// <param name="_metadata0"></param>
        /// <param name="_personrole1"></param>
        public Artwork(string path, Enums.ArtKind kind, Metadata _metadata0, PersonRole _personrole1)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            this.Path = path;

            this.Kind = kind;

            if (_metadata0 == null) throw new ArgumentNullException(nameof(_metadata0));
            _metadata0.Artwork.Add(this);

            if (_personrole1 == null) throw new ArgumentNullException(nameof(_personrole1));
            _personrole1.Artwork = this;


            Init();
        }

        /// <summary>
        /// Static create function (for use in LINQ queries, etc.)
        /// </summary>
        /// <param name="path"></param>
        /// <param name="kind"></param>
        /// <param name="_metadata0"></param>
        /// <param name="_personrole1"></param>
        public static Artwork Create(string path, Enums.ArtKind kind, Metadata _metadata0, PersonRole _personrole1)
        {
            return new Artwork(path, kind, _metadata0, _personrole1);
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
        internal Enums.ArtKind _Kind;
        /// <summary>
        /// When provided in a partial class, allows value of Kind to be changed before setting.
        /// </summary>
        partial void SetKind(Enums.ArtKind oldValue, ref Enums.ArtKind newValue);
        /// <summary>
        /// When provided in a partial class, allows value of Kind to be changed before returning.
        /// </summary>
        partial void GetKind(ref Enums.ArtKind result);

        /// <summary>
        /// Indexed, Required
        /// </summary>
        [Required]
        public Enums.ArtKind Kind
        {
            get
            {
                Enums.ArtKind value = _Kind;
                GetKind(ref value);
                return (_Kind = value);
            }
            set
            {
                Enums.ArtKind oldValue = _Kind;
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

    }
}

