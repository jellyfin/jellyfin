using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Jellyfin.Data.Interfaces;

namespace Jellyfin.Data.Entities.Libraries
{
    /// <summary>
    /// An entity representing a stream in a media file.
    /// </summary>
    public class MediaFileStream : IHasConcurrencyToken
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MediaFileStream"/> class.
        /// </summary>
        /// <param name="streamNumber">The number of this stream.</param>
        /// <param name="mediaFile">The media file.</param>
        public MediaFileStream(int streamNumber, MediaFile mediaFile)
        {
            StreamNumber = streamNumber;

            if (mediaFile == null)
            {
                throw new ArgumentNullException(nameof(mediaFile));
            }

            mediaFile.MediaFileStreams.Add(this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaFileStream"/> class.
        /// </summary>
        /// <remarks>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </remarks>
        protected MediaFileStream()
        {
        }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <remarks>
        /// Identity, Indexed, Required.
        /// </remarks>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; protected set; }

        /// <summary>
        /// Gets or sets the stream number.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public int StreamNumber { get; set; }

        /// <inheritdoc />
        [ConcurrencyCheck]
        public uint RowVersion { get; set; }

        /// <inheritdoc />
        public void OnSavingChanges()
        {
            RowVersion++;
        }
    }
}
