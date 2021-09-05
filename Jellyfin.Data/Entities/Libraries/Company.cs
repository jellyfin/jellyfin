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
        public Company()
        {
            CompanyMetadata = new HashSet<CompanyMetadata>();
            ChildCompanies = new HashSet<Company>();
        }

        /// <summary>
        /// Gets the id.
        /// </summary>
        /// <remarks>
        /// Identity, Indexed, Required.
        /// </remarks>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }

        /// <inheritdoc />
        [ConcurrencyCheck]
        public uint RowVersion { get; private set; }

        /// <summary>
        /// Gets a collection containing the metadata.
        /// </summary>
        public virtual ICollection<CompanyMetadata> CompanyMetadata { get; private set; }

        /// <summary>
        /// Gets a collection containing this company's child companies.
        /// </summary>
        public virtual ICollection<Company> ChildCompanies { get; private set; }

        /// <inheritdoc />
        public ICollection<Company> Companies => ChildCompanies;

        /// <inheritdoc />
        public void OnSavingChanges()
        {
            RowVersion++;
        }
    }
}
