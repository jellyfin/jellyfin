using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities
{
    public partial class MediaFileStream
    {
        partial void Init();

        /// <summary>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </summary>
        protected MediaFileStream()
        {
            Init();
        }

        /// <summary>
        /// Replaces default constructor, since it's protected. Caller assumes responsibility for setting all required values before saving.
        /// </summary>
        public static MediaFileStream CreateMediaFileStreamUnsafe()
        {
            return new MediaFileStream();
        }

        /// <summary>
        /// Public constructor with required data
        /// </summary>
        /// <param name="streamnumber"></param>
        /// <param name="_mediafile0"></param>
        public MediaFileStream(int streamnumber, MediaFile _mediafile0)
        {
            this.StreamNumber = streamnumber;

            if (_mediafile0 == null) throw new ArgumentNullException(nameof(_mediafile0));
            _mediafile0.MediaFileStreams.Add(this);


            Init();
        }

        /// <summary>
        /// Static create function (for use in LINQ queries, etc.)
        /// </summary>
        /// <param name="streamnumber"></param>
        /// <param name="_mediafile0"></param>
        public static MediaFileStream Create(int streamnumber, MediaFile _mediafile0)
        {
            return new MediaFileStream(streamnumber, _mediafile0);
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
        /// Backing field for StreamNumber
        /// </summary>
        protected int _StreamNumber;
        /// <summary>
        /// When provided in a partial class, allows value of StreamNumber to be changed before setting.
        /// </summary>
        partial void SetStreamNumber(int oldValue, ref int newValue);
        /// <summary>
        /// When provided in a partial class, allows value of StreamNumber to be changed before returning.
        /// </summary>
        partial void GetStreamNumber(ref int result);

        /// <summary>
        /// Required
        /// </summary>
        [Required]
        public int StreamNumber
        {
            get
            {
                int value = _StreamNumber;
                GetStreamNumber(ref value);
                return (_StreamNumber = value);
            }
            set
            {
                int oldValue = _StreamNumber;
                SetStreamNumber(oldValue, ref value);
                if (oldValue != value)
                {
                    _StreamNumber = value;
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

