using MediaBrowser.Model.Entities;

namespace MediaBrowser.Model.Querying
{
    /// <summary>
    /// Class PersonsQuery
    /// </summary>
    public class PersonsQuery : ItemsByNameQuery
    {
        /// <summary>
        /// Gets or sets the person types.
        /// </summary>
        /// <value>The person types.</value>
        public string[] PersonTypes { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PersonsQuery"/> class.
        /// </summary>
        public PersonsQuery()
        {
            PersonTypes = new string[] { };
        }
    }
}
