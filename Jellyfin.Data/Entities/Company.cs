using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities
{
    public partial class Company
    {
        partial void Init();

        /// <summary>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </summary>
        protected Company()
        {
            CompanyMetadata = new HashSet<CompanyMetadata>();

            Init();
        }

        /// <summary>
        /// Replaces default constructor, since it's protected. Caller assumes responsibility for setting all required values before saving.
        /// </summary>
        public static Company CreateCompanyUnsafe()
        {
            return new Company();
        }

        /// <summary>
        /// Public constructor with required data
        /// </summary>
        /// <param name="_moviemetadata0"></param>
        /// <param name="_seriesmetadata1"></param>
        /// <param name="_musicalbummetadata2"></param>
        /// <param name="_bookmetadata3"></param>
        /// <param name="_company4"></param>
        public Company(MovieMetadata _moviemetadata0, SeriesMetadata _seriesmetadata1, MusicAlbumMetadata _musicalbummetadata2, BookMetadata _bookmetadata3, Company _company4)
        {
            if (_moviemetadata0 == null) throw new ArgumentNullException(nameof(_moviemetadata0));
            _moviemetadata0.Studios.Add(this);

            if (_seriesmetadata1 == null) throw new ArgumentNullException(nameof(_seriesmetadata1));
            _seriesmetadata1.Networks.Add(this);

            if (_musicalbummetadata2 == null) throw new ArgumentNullException(nameof(_musicalbummetadata2));
            _musicalbummetadata2.Labels.Add(this);

            if (_bookmetadata3 == null) throw new ArgumentNullException(nameof(_bookmetadata3));
            _bookmetadata3.Publishers.Add(this);

            if (_company4 == null) throw new ArgumentNullException(nameof(_company4));
            _company4.Parent = this;

            this.CompanyMetadata = new HashSet<CompanyMetadata>();

            Init();
        }

        /// <summary>
        /// Static create function (for use in LINQ queries, etc.)
        /// </summary>
        /// <param name="_moviemetadata0"></param>
        /// <param name="_seriesmetadata1"></param>
        /// <param name="_musicalbummetadata2"></param>
        /// <param name="_bookmetadata3"></param>
        /// <param name="_company4"></param>
        public static Company Create(MovieMetadata _moviemetadata0, SeriesMetadata _seriesmetadata1, MusicAlbumMetadata _musicalbummetadata2, BookMetadata _bookmetadata3, Company _company4)
        {
            return new Company(_moviemetadata0, _seriesmetadata1, _musicalbummetadata2, _bookmetadata3, _company4);
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
        [ForeignKey("CompanyMetadata_CompanyMetadata_Id")]
        public virtual ICollection<CompanyMetadata> CompanyMetadata { get; protected set; }
        [ForeignKey("Company_Parent_Id")]
        public virtual Company Parent { get; set; }

    }
}

