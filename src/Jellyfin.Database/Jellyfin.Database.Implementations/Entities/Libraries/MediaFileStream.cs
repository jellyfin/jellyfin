#pragma warning disable CA1711 // Identifiers should not have incorrect suffix

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
        public MediaFileStream(int streamNumber)
        {
            StreamNumber = streamNumber;
        }

        /// <summary>
        /// Gets the id.
        /// </summary>
        /// <remarks>
        /// Identity, Indexed, Required.
        /// </remarks>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }

        /// <summary>
        /// Gets or sets the stream number.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public int StreamNumber { get; set; }

        /// <inheritdoc />
        [ConcurrencyCheck]
        public uint RowVersion { get; private set; }

        /// <inheritdoc />
        public void OnSavingChanges()
        {
            RowVersion++;
        }
    }
}
