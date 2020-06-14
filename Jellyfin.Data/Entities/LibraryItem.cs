using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities
{
    public abstract partial class LibraryItem
    {
        partial void Init();

        /// <summary>
        /// Default constructor. Protected due to being abstract.
        /// </summary>
        protected LibraryItem()
        {
            Init();
        }

        /// <summary>
        /// Public constructor with required data
        /// </summary>
        /// <param name="urlid">This is whats gets displayed in the Urls and API requests. This could also be a string.</param>
        protected LibraryItem(Guid urlid, DateTime dateadded)
        {
            this.UrlId = urlid;


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
        /// Backing field for UrlId
        /// </summary>
        internal Guid _UrlId;
        /// <summary>
        /// When provided in a partial class, allows value of UrlId to be changed before setting.
        /// </summary>
        partial void SetUrlId(Guid oldValue, ref Guid newValue);
        /// <summary>
        /// When provided in a partial class, allows value of UrlId to be changed before returning.
        /// </summary>
        partial void GetUrlId(ref Guid result);

        /// <summary>
        /// Indexed, Required
        /// This is whats gets displayed in the Urls and API requests. This could also be a string.
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
        [ForeignKey("LibraryRoot_Id")]
        public virtual LibraryRoot LibraryRoot { get; set; }

    }
}

