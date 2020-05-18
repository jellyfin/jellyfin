using System;
using System.ComponentModel.DataAnnotations;

namespace Jellyfin.Data.Entities
{
    public partial class CompanyMetadata : Metadata
    {
        partial void Init();

        /// <summary>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </summary>
        protected CompanyMetadata()
        {
            Init();
        }

        /// <summary>
        /// Replaces default constructor, since it's protected. Caller assumes responsibility for setting all required values before saving.
        /// </summary>
        public static CompanyMetadata CreateCompanyMetadataUnsafe()
        {
            return new CompanyMetadata();
        }

        /// <summary>
        /// Public constructor with required data
        /// </summary>
        /// <param name="title">The title or name of the object</param>
        /// <param name="language">ISO-639-3 3-character language codes</param>
        /// <param name="_company0"></param>
        public CompanyMetadata(string title, string language, DateTime dateadded, DateTime datemodified, Company _company0)
        {
            if (string.IsNullOrEmpty(title)) throw new ArgumentNullException(nameof(title));
            this.Title = title;

            if (string.IsNullOrEmpty(language)) throw new ArgumentNullException(nameof(language));
            this.Language = language;

            if (_company0 == null) throw new ArgumentNullException(nameof(_company0));
            _company0.CompanyMetadata.Add(this);


            Init();
        }

        /// <summary>
        /// Static create function (for use in LINQ queries, etc.)
        /// </summary>
        /// <param name="title">The title or name of the object</param>
        /// <param name="language">ISO-639-3 3-character language codes</param>
        /// <param name="_company0"></param>
        public static CompanyMetadata Create(string title, string language, DateTime dateadded, DateTime datemodified, Company _company0)
        {
            return new CompanyMetadata(title, language, dateadded, datemodified, _company0);
        }

        /*************************************************************************
         * Properties
         *************************************************************************/

        /// <summary>
        /// Backing field for Description
        /// </summary>
        protected string _Description;
        /// <summary>
        /// When provided in a partial class, allows value of Description to be changed before setting.
        /// </summary>
        partial void SetDescription(string oldValue, ref string newValue);
        /// <summary>
        /// When provided in a partial class, allows value of Description to be changed before returning.
        /// </summary>
        partial void GetDescription(ref string result);

        /// <summary>
        /// Max length = 65535
        /// </summary>
        [MaxLength(65535)]
        [StringLength(65535)]
        public string Description
        {
            get
            {
                string value = _Description;
                GetDescription(ref value);
                return (_Description = value);
            }
            set
            {
                string oldValue = _Description;
                SetDescription(oldValue, ref value);
                if (oldValue != value)
                {
                    _Description = value;
                }
            }
        }

        /// <summary>
        /// Backing field for Headquarters
        /// </summary>
        protected string _Headquarters;
        /// <summary>
        /// When provided in a partial class, allows value of Headquarters to be changed before setting.
        /// </summary>
        partial void SetHeadquarters(string oldValue, ref string newValue);
        /// <summary>
        /// When provided in a partial class, allows value of Headquarters to be changed before returning.
        /// </summary>
        partial void GetHeadquarters(ref string result);

        /// <summary>
        /// Max length = 255
        /// </summary>
        [MaxLength(255)]
        [StringLength(255)]
        public string Headquarters
        {
            get
            {
                string value = _Headquarters;
                GetHeadquarters(ref value);
                return (_Headquarters = value);
            }
            set
            {
                string oldValue = _Headquarters;
                SetHeadquarters(oldValue, ref value);
                if (oldValue != value)
                {
                    _Headquarters = value;
                }
            }
        }

        /// <summary>
        /// Backing field for Country
        /// </summary>
        protected string _Country;
        /// <summary>
        /// When provided in a partial class, allows value of Country to be changed before setting.
        /// </summary>
        partial void SetCountry(string oldValue, ref string newValue);
        /// <summary>
        /// When provided in a partial class, allows value of Country to be changed before returning.
        /// </summary>
        partial void GetCountry(ref string result);

        /// <summary>
        /// Max length = 2
        /// </summary>
        [MaxLength(2)]
        [StringLength(2)]
        public string Country
        {
            get
            {
                string value = _Country;
                GetCountry(ref value);
                return (_Country = value);
            }
            set
            {
                string oldValue = _Country;
                SetCountry(oldValue, ref value);
                if (oldValue != value)
                {
                    _Country = value;
                }
            }
        }

        /// <summary>
        /// Backing field for Homepage
        /// </summary>
        protected string _Homepage;
        /// <summary>
        /// When provided in a partial class, allows value of Homepage to be changed before setting.
        /// </summary>
        partial void SetHomepage(string oldValue, ref string newValue);
        /// <summary>
        /// When provided in a partial class, allows value of Homepage to be changed before returning.
        /// </summary>
        partial void GetHomepage(ref string result);

        /// <summary>
        /// Max length = 1024
        /// </summary>
        [MaxLength(1024)]
        [StringLength(1024)]
        public string Homepage
        {
            get
            {
                string value = _Homepage;
                GetHomepage(ref value);
                return (_Homepage = value);
            }
            set
            {
                string oldValue = _Homepage;
                SetHomepage(oldValue, ref value);
                if (oldValue != value)
                {
                    _Homepage = value;
                }
            }
        }

        /*************************************************************************
         * Navigation properties
         *************************************************************************/

    }
}

