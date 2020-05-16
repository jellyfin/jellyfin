using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities
{
    public partial class Person
    {
        partial void Init();

        /// <summary>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </summary>
        protected Person()
        {
            Sources = new HashSet<MetadataProviderId>();

            Init();
        }

        /// <summary>
        /// Replaces default constructor, since it's protected. Caller assumes responsibility for setting all required values before saving.
        /// </summary>
        public static Person CreatePersonUnsafe()
        {
            return new Person();
        }

        /// <summary>
        /// Public constructor with required data
        /// </summary>
        /// <param name="urlid"></param>
        /// <param name="name"></param>
        public Person(Guid urlid, string name, DateTime dateadded, DateTime datemodified)
        {
            this.UrlId = urlid;

            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            this.Name = name;

            this.Sources = new HashSet<MetadataProviderId>();

            Init();
        }

        /// <summary>
        /// Static create function (for use in LINQ queries, etc.)
        /// </summary>
        /// <param name="urlid"></param>
        /// <param name="name"></param>
        public static Person Create(Guid urlid, string name, DateTime dateadded, DateTime datemodified)
        {
            return new Person(urlid, name, dateadded, datemodified);
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
        /// Backing field for UrlId
        /// </summary>
        protected Guid _UrlId;
        /// <summary>
        /// When provided in a partial class, allows value of UrlId to be changed before setting.
        /// </summary>
        partial void SetUrlId(Guid oldValue, ref Guid newValue);
        /// <summary>
        /// When provided in a partial class, allows value of UrlId to be changed before returning.
        /// </summary>
        partial void GetUrlId(ref Guid result);

        /// <summary>
        /// Required
        /// </summary>
        [Required]
        public Guid UrlId
        {
            get
            {
                Guid value = _UrlId;
                GetUrlId(ref value);
                return (_UrlId = value);
            }
            set
            {
                Guid oldValue = _UrlId;
                SetUrlId(oldValue, ref value);
                if (oldValue != value)
                {
                    _UrlId = value;
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
        /// Backing field for SourceId
        /// </summary>
        protected string _SourceId;
        /// <summary>
        /// When provided in a partial class, allows value of SourceId to be changed before setting.
        /// </summary>
        partial void SetSourceId(string oldValue, ref string newValue);
        /// <summary>
        /// When provided in a partial class, allows value of SourceId to be changed before returning.
        /// </summary>
        partial void GetSourceId(ref string result);

        /// <summary>
        /// Max length = 255
        /// </summary>
        [MaxLength(255)]
        [StringLength(255)]
        public string SourceId
        {
            get
            {
                string value = _SourceId;
                GetSourceId(ref value);
                return (_SourceId = value);
            }
            set
            {
                string oldValue = _SourceId;
                SetSourceId(oldValue, ref value);
                if (oldValue != value)
                {
                    _SourceId = value;
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
        [ForeignKey("MetadataProviderId_Sources_Id")]
        public virtual ICollection<MetadataProviderId> Sources { get; protected set; }

    }
}

