using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities
{
    public partial class Library
    {
        partial void Init();

        /// <summary>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </summary>
        protected Library()
        {
            Init();
        }

        /// <summary>
        /// Replaces default constructor, since it's protected. Caller assumes responsibility for setting all required values before saving.
        /// </summary>
        public static Library CreateLibraryUnsafe()
        {
            return new Library();
        }

        /// <summary>
        /// Public constructor with required data
        /// </summary>
        /// <param name="name"></param>
        public Library(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            this.Name = name;


            Init();
        }

        /// <summary>
        /// Static create function (for use in LINQ queries, etc.)
        /// </summary>
        /// <param name="name"></param>
        public static Library Create(string name)
        {
            return new Library(name);
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

    }
}

