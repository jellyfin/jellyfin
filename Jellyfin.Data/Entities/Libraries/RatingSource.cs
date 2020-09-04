using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Jellyfin.Data.Interfaces;

namespace Jellyfin.Data.Entities.Libraries
{
    /// <summary>
    /// This is the entity to store review ratings, not age ratings.
    /// </summary>
    public class RatingSource : IHasConcurrencyToken
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RatingSource"/> class.
        /// </summary>
        /// <param name="minimumValue">The minimum value.</param>
        /// <param name="maximumValue">The maximum value.</param>
        /// <param name="rating">The rating.</param>
        public RatingSource(double minimumValue, double maximumValue, Rating rating)
        {
            MinimumValue = minimumValue;
            MaximumValue = maximumValue;

            if (rating == null)
            {
                throw new ArgumentNullException(nameof(rating));
            }

            rating.RatingType = this;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RatingSource"/> class.
        /// </summary>
        /// <remarks>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </remarks>
        protected RatingSource()
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
        /// Gets or sets the name.
        /// </summary>
        /// <remarks>
        /// Max length = 1024.
        /// </remarks>
        [MaxLength(1024)]
        [StringLength(1024)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the minimum value.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public double MinimumValue { get; set; }

        /// <summary>
        /// Gets or sets the maximum value.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public double MaximumValue { get; set; }

        /// <inheritdoc />
        [ConcurrencyCheck]
        public uint RowVersion { get; set; }

        /// <summary>
        /// Gets or sets the metadata source.
        /// </summary>
        public virtual MetadataProviderId Source { get; set; }

        /// <inheritdoc />
        public void OnSavingChanges()
        {
            RowVersion++;
        }
    }
}
