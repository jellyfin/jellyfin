using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities
{
    public partial class Collection
    {
        partial void Init();

        /// <summary>
        /// Default constructor
        /// </summary>
        public Collection()
        {
            CollectionItem = new LinkedList<CollectionItem>();

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
        /// Max length = 1024
        /// </summary>
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
        [ForeignKey("CollectionItem_CollectionItem_Id")]
        public virtual ICollection<CollectionItem> CollectionItem { get; protected set; }

    }
}

