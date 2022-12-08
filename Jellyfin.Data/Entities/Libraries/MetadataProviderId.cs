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
        /// <param name="metadataProvider">The metadata provider.</param>
        public MetadataProviderId(string providerId, MetadataProvider metadataProvider)
        {
            ArgumentException.ThrowIfNullOrEmpty(providerId);

            ProviderId = providerId;
            MetadataProvider = metadataProvider;
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
        /// Gets or sets the provider id.
        /// </summary>
        /// <remarks>
        /// Required, Max length = 255.
        /// </remarks>
        [MaxLength(255)]
        [StringLength(255)]
        public string ProviderId { get; set; }

        /// <inheritdoc />
        [ConcurrencyCheck]
        public uint RowVersion { get; private set; }

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
