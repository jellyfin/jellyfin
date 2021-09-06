using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Jellyfin.Data.Interfaces;

namespace Jellyfin.Data.Entities.Libraries
{
    /// <summary>
    /// An entity representing a rating for an entity.
    /// </summary>
    public class Rating : IHasConcurrencyToken
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Rating"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        public Rating(double value)
        {
            Value = value;
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
        /// Gets or sets the value.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public double Value { get; set; }

        /// <summary>
        /// Gets or sets the number of votes.
        /// </summary>
        public int? Votes { get; set; }

        /// <inheritdoc />
        [ConcurrencyCheck]
        public uint RowVersion { get; private set; }

        /// <summary>
        /// Gets or sets the rating type.
        /// If this is <c>null</c> it's the internal user rating.
        /// </summary>
        public virtual RatingSource? RatingType { get; set; }

        /// <inheritdoc />
        public void OnSavingChanges()
        {
            RowVersion++;
        }
    }
}
