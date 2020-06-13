using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities
{
    /// <summary>
    /// This is the entity to store review ratings, not age ratings
    /// </summary>
    public partial class RatingSource
    {
        partial void Init();

        /// <summary>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </summary>
        protected RatingSource()
        {
            Init();
        }

        /// <summary>
        /// Replaces default constructor, since it's protected. Caller assumes responsibility for setting all required values before saving.
        /// </summary>
        public static RatingSource CreateRatingSourceUnsafe()
        {
            return new RatingSource();
        }

        /// <summary>
        /// Public constructor with required data
        /// </summary>
        /// <param name="maximumvalue"></param>
        /// <param name="minimumvalue"></param>
        /// <param name="_rating0"></param>
        public RatingSource(double maximumvalue, double minimumvalue, Rating _rating0)
        {
            this.MaximumValue = maximumvalue;

            this.MinimumValue = minimumvalue;

            if (_rating0 == null) throw new ArgumentNullException(nameof(_rating0));
            _rating0.RatingType = this;


            Init();
        }

        /// <summary>
        /// Static create function (for use in LINQ queries, etc.)
        /// </summary>
        /// <param name="maximumvalue"></param>
        /// <param name="minimumvalue"></param>
        /// <param name="_rating0"></param>
        public static RatingSource Create(double maximumvalue, double minimumvalue, Rating _rating0)
        {
            return new RatingSource(maximumvalue, minimumvalue, _rating0);
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
        /// Backing field for MaximumValue
        /// </summary>
        protected double _MaximumValue;
        /// <summary>
        /// When provided in a partial class, allows value of MaximumValue to be changed before setting.
        /// </summary>
        partial void SetMaximumValue(double oldValue, ref double newValue);
        /// <summary>
        /// When provided in a partial class, allows value of MaximumValue to be changed before returning.
        /// </summary>
        partial void GetMaximumValue(ref double result);

        /// <summary>
        /// Required
        /// </summary>
        [Required]
        public double MaximumValue
        {
            get
            {
                double value = _MaximumValue;
                GetMaximumValue(ref value);
                return (_MaximumValue = value);
            }
            set
            {
                double oldValue = _MaximumValue;
                SetMaximumValue(oldValue, ref value);
                if (oldValue != value)
                {
                    _MaximumValue = value;
                }
            }
        }

        /// <summary>
        /// Backing field for MinimumValue
        /// </summary>
        protected double _MinimumValue;
        /// <summary>
        /// When provided in a partial class, allows value of MinimumValue to be changed before setting.
        /// </summary>
        partial void SetMinimumValue(double oldValue, ref double newValue);
        /// <summary>
        /// When provided in a partial class, allows value of MinimumValue to be changed before returning.
        /// </summary>
        partial void GetMinimumValue(ref double result);

        /// <summary>
        /// Required
        /// </summary>
        [Required]
        public double MinimumValue
        {
            get
            {
                double value = _MinimumValue;
                GetMinimumValue(ref value);
                return (_MinimumValue = value);
            }
            set
            {
                double oldValue = _MinimumValue;
                SetMinimumValue(oldValue, ref value);
                if (oldValue != value)
                {
                    _MinimumValue = value;
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
        [ForeignKey("MetadataProviderId_Source_Id")]
        public virtual MetadataProviderId Source { get; set; }

    }
}

