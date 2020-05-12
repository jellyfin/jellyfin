using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities
{
    public partial class Chapter
    {
        partial void Init();

        /// <summary>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </summary>
        protected Chapter()
        {
            Init();
        }

        /// <summary>
        /// Replaces default constructor, since it's protected. Caller assumes responsibility for setting all required values before saving.
        /// </summary>
        public static Chapter CreateChapterUnsafe()
        {
            return new Chapter();
        }

        /// <summary>
        /// Public constructor with required data
        /// </summary>
        /// <param name="language">ISO-639-3 3-character language codes</param>
        /// <param name="timestart"></param>
        /// <param name="_release0"></param>
        public Chapter(string language, long timestart, Release _release0)
        {
            if (string.IsNullOrEmpty(language)) throw new ArgumentNullException(nameof(language));
            this.Language = language;

            this.TimeStart = timestart;

            if (_release0 == null) throw new ArgumentNullException(nameof(_release0));
            _release0.Chapters.Add(this);


            Init();
        }

        /// <summary>
        /// Static create function (for use in LINQ queries, etc.)
        /// </summary>
        /// <param name="language">ISO-639-3 3-character language codes</param>
        /// <param name="timestart"></param>
        /// <param name="_release0"></param>
        public static Chapter Create(string language, long timestart, Release _release0)
        {
            return new Chapter(language, timestart, _release0);
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
        /// Backing field for Language
        /// </summary>
        protected string _Language;
        /// <summary>
        /// When provided in a partial class, allows value of Language to be changed before setting.
        /// </summary>
        partial void SetLanguage(string oldValue, ref string newValue);
        /// <summary>
        /// When provided in a partial class, allows value of Language to be changed before returning.
        /// </summary>
        partial void GetLanguage(ref string result);

        /// <summary>
        /// Required, Min length = 3, Max length = 3
        /// ISO-639-3 3-character language codes
        /// </summary>
        [Required]
        [MinLength(3)]
        [MaxLength(3)]
        [StringLength(3)]
        public string Language
        {
            get
            {
                string value = _Language;
                GetLanguage(ref value);
                return (_Language = value);
            }
            set
            {
                string oldValue = _Language;
                SetLanguage(oldValue, ref value);
                if (oldValue != value)
                {
                    _Language = value;
                }
            }
        }

        /// <summary>
        /// Backing field for TimeStart
        /// </summary>
        protected long _TimeStart;
        /// <summary>
        /// When provided in a partial class, allows value of TimeStart to be changed before setting.
        /// </summary>
        partial void SetTimeStart(long oldValue, ref long newValue);
        /// <summary>
        /// When provided in a partial class, allows value of TimeStart to be changed before returning.
        /// </summary>
        partial void GetTimeStart(ref long result);

        /// <summary>
        /// Required
        /// </summary>
        [Required]
        public long TimeStart
        {
            get
            {
                long value = _TimeStart;
                GetTimeStart(ref value);
                return (_TimeStart = value);
            }
            set
            {
                long oldValue = _TimeStart;
                SetTimeStart(oldValue, ref value);
                if (oldValue != value)
                {
                    _TimeStart = value;
                }
            }
        }

        /// <summary>
        /// Backing field for TimeEnd
        /// </summary>
        protected long? _TimeEnd;
        /// <summary>
        /// When provided in a partial class, allows value of TimeEnd to be changed before setting.
        /// </summary>
        partial void SetTimeEnd(long? oldValue, ref long? newValue);
        /// <summary>
        /// When provided in a partial class, allows value of TimeEnd to be changed before returning.
        /// </summary>
        partial void GetTimeEnd(ref long? result);

        public long? TimeEnd
        {
            get
            {
                long? value = _TimeEnd;
                GetTimeEnd(ref value);
                return (_TimeEnd = value);
            }
            set
            {
                long? oldValue = _TimeEnd;
                SetTimeEnd(oldValue, ref value);
                if (oldValue != value)
                {
                    _TimeEnd = value;
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

