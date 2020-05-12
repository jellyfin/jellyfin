using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities
{
    public partial class CollectionItem
    {
        partial void Init();

        /// <summary>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </summary>
        protected CollectionItem()
        {
            // NOTE: This class has one-to-one associations with CollectionItem.
            // One-to-one associations are not validated in constructors since this causes a scenario where each one must be constructed before the other.

            Init();
        }

        /// <summary>
        /// Replaces default constructor, since it's protected. Caller assumes responsibility for setting all required values before saving.
        /// </summary>
        public static CollectionItem CreateCollectionItemUnsafe()
        {
            return new CollectionItem();
        }

        /// <summary>
        /// Public constructor with required data
        /// </summary>
        /// <param name="_collection0"></param>
        /// <param name="_collectionitem1"></param>
        /// <param name="_collectionitem2"></param>
        public CollectionItem(Collection _collection0, CollectionItem _collectionitem1, CollectionItem _collectionitem2)
        {
            // NOTE: This class has one-to-one associations with CollectionItem.
            // One-to-one associations are not validated in constructors since this causes a scenario where each one must be constructed before the other.

            if (_collection0 == null) throw new ArgumentNullException(nameof(_collection0));
            _collection0.CollectionItem.Add(this);

            if (_collectionitem1 == null) throw new ArgumentNullException(nameof(_collectionitem1));
            _collectionitem1.Next = this;

            if (_collectionitem2 == null) throw new ArgumentNullException(nameof(_collectionitem2));
            _collectionitem2.Previous = this;


            Init();
        }

        /// <summary>
        /// Static create function (for use in LINQ queries, etc.)
        /// </summary>
        /// <param name="_collection0"></param>
        /// <param name="_collectionitem1"></param>
        /// <param name="_collectionitem2"></param>
        public static CollectionItem Create(Collection _collection0, CollectionItem _collectionitem1, CollectionItem _collectionitem2)
        {
            return new CollectionItem(_collection0, _collectionitem1, _collectionitem2);
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
        [ForeignKey("LibraryItem_Id")]
        public virtual LibraryItem LibraryItem { get; set; }

        /// <remarks>
        /// TODO check if this properly updated dependant and has the proper principal relationship
        /// </remarks>
        [ForeignKey("CollectionItem_Next_Id")]
        public virtual CollectionItem Next { get; set; }

        /// <remarks>
        /// TODO check if this properly updated dependant and has the proper principal relationship
        /// </remarks>
        [ForeignKey("CollectionItem_Previous_Id")]
        public virtual CollectionItem Previous { get; set; }

    }
}

