#pragma warning disable CA2227

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Jellyfin.Data.Interfaces;

namespace Jellyfin.Data.Entities.Libraries
{
    /// <summary>
    /// An entity representing a company.
    /// </summary>
    public class Company : IHasCompanies, IHasConcurrencyToken
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Company"/> class.
        /// </summary>
        /// <param name="owner">The owner of this company.</param>
        public Company(IHasCompanies owner)
        {
            owner?.Companies.Add(this);

            CompanyMetadata = new HashSet<CompanyMetadata>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Company"/> class.
        /// </summary>
        /// <remarks>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </remarks>
        protected Company()
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

        /// <inheritdoc />
        [ConcurrencyCheck]
        public uint RowVersion { get; set; }

        /// <summary>
        /// Gets or sets a collection containing the metadata.
        /// </summary>
        public virtual ICollection<CompanyMetadata> CompanyMetadata { get; protected set; }

        /// <summary>
        /// Gets or sets a collection containing this company's child companies.
        /// </summary>
        public virtual ICollection<Company> ChildCompanies { get; protected set; }

        /// <inheritdoc />
        [NotMapped]
        public ICollection<Company> Companies => ChildCompanies;

        /// <inheritdoc />
        public void OnSavingChanges()
        {
            RowVersion++;
        }
    }
}
