using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities
{
    public partial class Rating
    {
        partial void Init();

        /// <summary>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </summary>
        protected Rating()
        {
            Init();
        }

        /// <summary>
        /// Replaces default constructor, since it's protected. Caller assumes responsibility for setting all required values before saving.
        /// </summary>
        public static Rating CreateRatingUnsafe()
        {
            return new Rating();
        }

        /// <summary>
        /// Public constructor with required data
        /// </summary>
        /// <param name="value"></param>
        /// <param name="_metadata0"></param>
        public Rating(double value, Metadata _metadata0)
        {
            this.Value = value;

            if (_metadata0 == null) throw new ArgumentNullException(nameof(_metadata0));
            _metadata0.Ratings.Add(this);


            Init();
        }

        /// <summary>
        /// Static create function (for use in LINQ queries, etc.)
        /// </summary>
        /// <param name="value"></param>
        /// <param name="_metadata0"></param>
        public static Rating Create(double value, Metadata _metadata0)
        {
            return new Rating(value, _metadata0);
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
        /// Backing field for Value
        /// </summary>
        protected double _Value;
        /// <summary>
        /// When provided in a partial class, allows value of Value to be changed before setting.
        /// </summary>
        partial void SetValue(double oldValue, ref double newValue);
        /// <summary>
        /// When provided in a partial class, allows value of Value to be changed before returning.
        /// </summary>
        partial void GetValue(ref double result);

        /// <summary>
        /// Required
        /// </summary>
        [Required]
        public double Value
        {
            get
            {
                double value = _Value;
                GetValue(ref value);
                return (_Value = value);
            }
            set
            {
                double oldValue = _Value;
                SetValue(oldValue, ref value);
                if (oldValue != value)
                {
                    _Value = value;
                }
            }
        }

        /// <summary>
        /// Backing field for Votes
        /// </summary>
        protected int? _Votes;
        /// <summary>
        /// When provided in a partial class, allows value of Votes to be changed before setting.
        /// </summary>
        partial void SetVotes(int? oldValue, ref int? newValue);
        /// <summary>
        /// When provided in a partial class, allows value of Votes to be changed before returning.
        /// </summary>
        partial void GetVotes(ref int? result);

        public int? Votes
        {
            get
            {
                int? value = _Votes;
                GetVotes(ref value);
                return (_Votes = value);
            }
            set
            {
                int? oldValue = _Votes;
                SetVotes(oldValue, ref value);
                if (oldValue != value)
                {
                    _Votes = value;
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
        /// If this is NULL it&apos;s the internal user rating.
        /// </summary>
        [ForeignKey("RatingSource_RatingType_Id")]
        public virtual RatingSource RatingType { get; set; }

    }
}

