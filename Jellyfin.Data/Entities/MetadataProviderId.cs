using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities
{
    public partial class MetadataProviderId
    {
        partial void Init();

        /// <summary>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </summary>
        protected MetadataProviderId()
        {
            // NOTE: This class has one-to-one associations with MetadataProviderId.
            // One-to-one associations are not validated in constructors since this causes a scenario where each one must be constructed before the other.

            Init();
        }

        /// <summary>
        /// Replaces default constructor, since it's protected. Caller assumes responsibility for setting all required values before saving.
        /// </summary>
        public static MetadataProviderId CreateMetadataProviderIdUnsafe()
        {
            return new MetadataProviderId();
        }

        /// <summary>
        /// Public constructor with required data
        /// </summary>
        /// <param name="providerid"></param>
        /// <param name="_metadata0"></param>
        /// <param name="_person1"></param>
        /// <param name="_personrole2"></param>
        /// <param name="_ratingsource3"></param>
        public MetadataProviderId(string providerid, Metadata _metadata0, Person _person1, PersonRole _personrole2, RatingSource _ratingsource3)
        {
            // NOTE: This class has one-to-one associations with MetadataProviderId.
            // One-to-one associations are not validated in constructors since this causes a scenario where each one must be constructed before the other.

            if (string.IsNullOrEmpty(providerid)) throw new ArgumentNullException(nameof(providerid));
            this.ProviderId = providerid;

            if (_metadata0 == null) throw new ArgumentNullException(nameof(_metadata0));
            _metadata0.Sources.Add(this);

            if (_person1 == null) throw new ArgumentNullException(nameof(_person1));
            _person1.Sources.Add(this);

            if (_personrole2 == null) throw new ArgumentNullException(nameof(_personrole2));
            _personrole2.Sources.Add(this);

            if (_ratingsource3 == null) throw new ArgumentNullException(nameof(_ratingsource3));
            _ratingsource3.Source = this;


            Init();
        }

        /// <summary>
        /// Static create function (for use in LINQ queries, etc.)
        /// </summary>
        /// <param name="providerid"></param>
        /// <param name="_metadata0"></param>
        /// <param name="_person1"></param>
        /// <param name="_personrole2"></param>
        /// <param name="_ratingsource3"></param>
        public static MetadataProviderId Create(string providerid, Metadata _metadata0, Person _person1, PersonRole _personrole2, RatingSource _ratingsource3)
        {
            return new MetadataProviderId(providerid, _metadata0, _person1, _personrole2, _ratingsource3);
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
        /// Backing field for ProviderId
        /// </summary>
        protected string _ProviderId;
        /// <summary>
        /// When provided in a partial class, allows value of ProviderId to be changed before setting.
        /// </summary>
        partial void SetProviderId(string oldValue, ref string newValue);
        /// <summary>
        /// When provided in a partial class, allows value of ProviderId to be changed before returning.
        /// </summary>
        partial void GetProviderId(ref string result);

        /// <summary>
        /// Required, Max length = 255
        /// </summary>
        [Required]
        [MaxLength(255)]
        [StringLength(255)]
        public string ProviderId
        {
            get
            {
                string value = _ProviderId;
                GetProviderId(ref value);
                return (_ProviderId = value);
            }
            set
            {
                string oldValue = _ProviderId;
                SetProviderId(oldValue, ref value);
                if (oldValue != value)
                {
                    _ProviderId = value;
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
        [ForeignKey("MetadataProvider_Id")]
        public virtual MetadataProvider MetadataProvider { get; set; }

    }
}

