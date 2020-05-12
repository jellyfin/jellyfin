using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities
{
    public partial class Series : LibraryItem
    {
        partial void Init();

        /// <summary>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </summary>
        protected Series()
        {
            SeriesMetadata = new HashSet<SeriesMetadata>();
            Seasons = new HashSet<Season>();

            Init();
        }

        /// <summary>
        /// Replaces default constructor, since it's protected. Caller assumes responsibility for setting all required values before saving.
        /// </summary>
        public static Series CreateSeriesUnsafe()
        {
            return new Series();
        }

        /// <summary>
        /// Public constructor with required data
        /// </summary>
        /// <param name="urlid">This is whats gets displayed in the Urls and API requests. This could also be a string.</param>
        public Series(Guid urlid, DateTime dateadded)
        {
            this.UrlId = urlid;

            this.SeriesMetadata = new HashSet<SeriesMetadata>();
            this.Seasons = new HashSet<Season>();

            Init();
        }

        /// <summary>
        /// Static create function (for use in LINQ queries, etc.)
        /// </summary>
        /// <param name="urlid">This is whats gets displayed in the Urls and API requests. This could also be a string.</param>
        public static Series Create(Guid urlid, DateTime dateadded)
        {
            return new Series(urlid, dateadded);
        }

        /*************************************************************************
         * Properties
         *************************************************************************/

        /// <summary>
        /// Backing field for AirsDayOfWeek
        /// </summary>
        protected Enums.Weekday? _AirsDayOfWeek;
        /// <summary>
        /// When provided in a partial class, allows value of AirsDayOfWeek to be changed before setting.
        /// </summary>
        partial void SetAirsDayOfWeek(Enums.Weekday? oldValue, ref Enums.Weekday? newValue);
        /// <summary>
        /// When provided in a partial class, allows value of AirsDayOfWeek to be changed before returning.
        /// </summary>
        partial void GetAirsDayOfWeek(ref Enums.Weekday? result);

        public Enums.Weekday? AirsDayOfWeek
        {
            get
            {
                Enums.Weekday? value = _AirsDayOfWeek;
                GetAirsDayOfWeek(ref value);
                return (_AirsDayOfWeek = value);
            }
            set
            {
                Enums.Weekday? oldValue = _AirsDayOfWeek;
                SetAirsDayOfWeek(oldValue, ref value);
                if (oldValue != value)
                {
                    _AirsDayOfWeek = value;
                }
            }
        }

        /// <summary>
        /// Backing field for AirsTime
        /// </summary>
        protected DateTimeOffset? _AirsTime;
        /// <summary>
        /// When provided in a partial class, allows value of AirsTime to be changed before setting.
        /// </summary>
        partial void SetAirsTime(DateTimeOffset? oldValue, ref DateTimeOffset? newValue);
        /// <summary>
        /// When provided in a partial class, allows value of AirsTime to be changed before returning.
        /// </summary>
        partial void GetAirsTime(ref DateTimeOffset? result);

        /// <summary>
        /// The time the show airs, ignore the date portion
        /// </summary>
        public DateTimeOffset? AirsTime
        {
            get
            {
                DateTimeOffset? value = _AirsTime;
                GetAirsTime(ref value);
                return (_AirsTime = value);
            }
            set
            {
                DateTimeOffset? oldValue = _AirsTime;
                SetAirsTime(oldValue, ref value);
                if (oldValue != value)
                {
                    _AirsTime = value;
                }
            }
        }

        /// <summary>
        /// Backing field for FirstAired
        /// </summary>
        protected DateTimeOffset? _FirstAired;
        /// <summary>
        /// When provided in a partial class, allows value of FirstAired to be changed before setting.
        /// </summary>
        partial void SetFirstAired(DateTimeOffset? oldValue, ref DateTimeOffset? newValue);
        /// <summary>
        /// When provided in a partial class, allows value of FirstAired to be changed before returning.
        /// </summary>
        partial void GetFirstAired(ref DateTimeOffset? result);

        public DateTimeOffset? FirstAired
        {
            get
            {
                DateTimeOffset? value = _FirstAired;
                GetFirstAired(ref value);
                return (_FirstAired = value);
            }
            set
            {
                DateTimeOffset? oldValue = _FirstAired;
                SetFirstAired(oldValue, ref value);
                if (oldValue != value)
                {
                    _FirstAired = value;
                }
            }
        }

        /*************************************************************************
         * Navigation properties
         *************************************************************************/
        [ForeignKey("SeriesMetadata_SeriesMetadata_Id")]
        public virtual ICollection<SeriesMetadata> SeriesMetadata { get; protected set; }

        [ForeignKey("Season_Seasons_Id")]
        public virtual ICollection<Season> Seasons { get; protected set; }

    }
}

