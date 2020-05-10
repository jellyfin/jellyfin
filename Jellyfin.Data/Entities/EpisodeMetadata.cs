using System;
using System.ComponentModel.DataAnnotations;

namespace Jellyfin.Data.Entities
{
    public partial class EpisodeMetadata : Metadata
    {
        partial void Init();

        /// <summary>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </summary>
        protected EpisodeMetadata()
        {
            Init();
        }

        /// <summary>
        /// Replaces default constructor, since it's protected. Caller assumes responsibility for setting all required values before saving.
        /// </summary>
        public static EpisodeMetadata CreateEpisodeMetadataUnsafe()
        {
            return new EpisodeMetadata();
        }

        /// <summary>
        /// Public constructor with required data
        /// </summary>
        /// <param name="title">The title or name of the object</param>
        /// <param name="language">ISO-639-3 3-character language codes</param>
        /// <param name="_episode0"></param>
        public EpisodeMetadata(string title, string language, DateTime dateadded, DateTime datemodified, Episode _episode0)
        {
            if (string.IsNullOrEmpty(title)) throw new ArgumentNullException(nameof(title));
            this.Title = title;

            if (string.IsNullOrEmpty(language)) throw new ArgumentNullException(nameof(language));
            this.Language = language;

            if (_episode0 == null) throw new ArgumentNullException(nameof(_episode0));
            _episode0.EpisodeMetadata.Add(this);


            Init();
        }

        /// <summary>
        /// Static create function (for use in LINQ queries, etc.)
        /// </summary>
        /// <param name="title">The title or name of the object</param>
        /// <param name="language">ISO-639-3 3-character language codes</param>
        /// <param name="_episode0"></param>
        public static EpisodeMetadata Create(string title, string language, DateTime dateadded, DateTime datemodified, Episode _episode0)
        {
            return new EpisodeMetadata(title, language, dateadded, datemodified, _episode0);
        }

        /*************************************************************************
         * Properties
         *************************************************************************/

        /// <summary>
        /// Backing field for Outline
        /// </summary>
        protected string _Outline;
        /// <summary>
        /// When provided in a partial class, allows value of Outline to be changed before setting.
        /// </summary>
        partial void SetOutline(string oldValue, ref string newValue);
        /// <summary>
        /// When provided in a partial class, allows value of Outline to be changed before returning.
        /// </summary>
        partial void GetOutline(ref string result);

        /// <summary>
        /// Max length = 1024
        /// </summary>
        [MaxLength(1024)]
        [StringLength(1024)]
        public string Outline
        {
            get
            {
                string value = _Outline;
                GetOutline(ref value);
                return (_Outline = value);
            }
            set
            {
                string oldValue = _Outline;
                SetOutline(oldValue, ref value);
                if (oldValue != value)
                {
                    _Outline = value;
                }
            }
        }

        /// <summary>
        /// Backing field for Plot
        /// </summary>
        protected string _Plot;
        /// <summary>
        /// When provided in a partial class, allows value of Plot to be changed before setting.
        /// </summary>
        partial void SetPlot(string oldValue, ref string newValue);
        /// <summary>
        /// When provided in a partial class, allows value of Plot to be changed before returning.
        /// </summary>
        partial void GetPlot(ref string result);

        /// <summary>
        /// Max length = 65535
        /// </summary>
        [MaxLength(65535)]
        [StringLength(65535)]
        public string Plot
        {
            get
            {
                string value = _Plot;
                GetPlot(ref value);
                return (_Plot = value);
            }
            set
            {
                string oldValue = _Plot;
                SetPlot(oldValue, ref value);
                if (oldValue != value)
                {
                    _Plot = value;
                }
            }
        }

        /// <summary>
        /// Backing field for Tagline
        /// </summary>
        protected string _Tagline;
        /// <summary>
        /// When provided in a partial class, allows value of Tagline to be changed before setting.
        /// </summary>
        partial void SetTagline(string oldValue, ref string newValue);
        /// <summary>
        /// When provided in a partial class, allows value of Tagline to be changed before returning.
        /// </summary>
        partial void GetTagline(ref string result);

        /// <summary>
        /// Max length = 1024
        /// </summary>
        [MaxLength(1024)]
        [StringLength(1024)]
        public string Tagline
        {
            get
            {
                string value = _Tagline;
                GetTagline(ref value);
                return (_Tagline = value);
            }
            set
            {
                string oldValue = _Tagline;
                SetTagline(oldValue, ref value);
                if (oldValue != value)
                {
                    _Tagline = value;
                }
            }
        }

        /*************************************************************************
         * Navigation properties
         *************************************************************************/

    }
}

