using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Jellyfin.Data.Interfaces;

namespace Jellyfin.Data.Entities.Libraries
{
    /// <summary>
    /// An entity representing a unique identifier for a metadata provider.
    /// </summary>
    public class MetadataProviderId : IHasConcurrencyToken
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataProviderId"/> class.
        /// </summary>
        /// <param name="providerId">The provider id.</param>
        /// <param name="itemMetadata">The metadata entity.</param>
        public MetadataProviderId(string providerId, ItemMetadata itemMetadata)
        {
            if (string.IsNullOrEmpty(providerId))
            {
                throw new ArgumentNullException(nameof(providerId));
            }

            ProviderId = providerId;

            if (itemMetadata == null)
            {
                throw new ArgumentNullException(nameof(itemMetadata));
            }

            itemMetadata.Sources.Add(this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataProviderId"/> class.
        /// </summary>
        /// <remarks>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </remarks>
        protected MetadataProviderId()
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
        /// Gets or sets the provider id.
        /// </summary>
        /// <remarks>
        /// Required, Max length = 255.
        /// </remarks>
        [Required]
        [MaxLength(255)]
        [StringLength(255)]
        public string ProviderId { get; set; }

        /// <inheritdoc />
        [ConcurrencyCheck]
        public uint RowVersion { get; set; }

        /// <summary>
        /// Gets or sets the metadata provider.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public virtual MetadataProvider MetadataProvider { get; set; }

        /// <inheritdoc />
        public void OnSavingChanges()
        {
            RowVersion++;
        }
    }
}
