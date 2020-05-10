using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities
{
    public partial class MusicAlbumMetadata : Metadata
    {
        partial void Init();

        /// <summary>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </summary>
        protected MusicAlbumMetadata()
        {
            Labels = new HashSet<Company>();

            Init();
        }

        /// <summary>
        /// Replaces default constructor, since it's protected. Caller assumes responsibility for setting all required values before saving.
        /// </summary>
        public static MusicAlbumMetadata CreateMusicAlbumMetadataUnsafe()
        {
            return new MusicAlbumMetadata();
        }

        /// <summary>
        /// Public constructor with required data
        /// </summary>
        /// <param name="title">The title or name of the object</param>
        /// <param name="language">ISO-639-3 3-character language codes</param>
        /// <param name="_musicalbum0"></param>
        public MusicAlbumMetadata(string title, string language, DateTime dateadded, DateTime datemodified, MusicAlbum _musicalbum0)
        {
            if (string.IsNullOrEmpty(title)) throw new ArgumentNullException(nameof(title));
            this.Title = title;

            if (string.IsNullOrEmpty(language)) throw new ArgumentNullException(nameof(language));
            this.Language = language;

            if (_musicalbum0 == null) throw new ArgumentNullException(nameof(_musicalbum0));
            _musicalbum0.MusicAlbumMetadata.Add(this);

            this.Labels = new HashSet<Company>();

            Init();
        }

        /// <summary>
        /// Static create function (for use in LINQ queries, etc.)
        /// </summary>
        /// <param name="title">The title or name of the object</param>
        /// <param name="language">ISO-639-3 3-character language codes</param>
        /// <param name="_musicalbum0"></param>
        public static MusicAlbumMetadata Create(string title, string language, DateTime dateadded, DateTime datemodified, MusicAlbum _musicalbum0)
        {
            return new MusicAlbumMetadata(title, language, dateadded, datemodified, _musicalbum0);
        }

        /*************************************************************************
         * Properties
         *************************************************************************/

        /// <summary>
        /// Backing field for Barcode
        /// </summary>
        protected string _Barcode;
        /// <summary>
        /// When provided in a partial class, allows value of Barcode to be changed before setting.
        /// </summary>
        partial void SetBarcode(string oldValue, ref string newValue);
        /// <summary>
        /// When provided in a partial class, allows value of Barcode to be changed before returning.
        /// </summary>
        partial void GetBarcode(ref string result);

        /// <summary>
        /// Max length = 255
        /// </summary>
        [MaxLength(255)]
        [StringLength(255)]
        public string Barcode
        {
            get
            {
                string value = _Barcode;
                GetBarcode(ref value);
                return (_Barcode = value);
            }
            set
            {
                string oldValue = _Barcode;
                SetBarcode(oldValue, ref value);
                if (oldValue != value)
                {
                    _Barcode = value;
                }
            }
        }

        /// <summary>
        /// Backing field for LabelNumber
        /// </summary>
        protected string _LabelNumber;
        /// <summary>
        /// When provided in a partial class, allows value of LabelNumber to be changed before setting.
        /// </summary>
        partial void SetLabelNumber(string oldValue, ref string newValue);
        /// <summary>
        /// When provided in a partial class, allows value of LabelNumber to be changed before returning.
        /// </summary>
        partial void GetLabelNumber(ref string result);

        /// <summary>
        /// Max length = 255
        /// </summary>
        [MaxLength(255)]
        [StringLength(255)]
        public string LabelNumber
        {
            get
            {
                string value = _LabelNumber;
                GetLabelNumber(ref value);
                return (_LabelNumber = value);
            }
            set
            {
                string oldValue = _LabelNumber;
                SetLabelNumber(oldValue, ref value);
                if (oldValue != value)
                {
                    _LabelNumber = value;
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

        /*************************************************************************
         * Navigation properties
         *************************************************************************/

        [ForeignKey("Company_Labels_Id")]
        public virtual ICollection<Company> Labels { get; protected set; }

    }
}

