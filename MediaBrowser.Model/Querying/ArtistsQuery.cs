
namespace MediaBrowser.Model.Querying
{
    /// <summary>
    /// Class ArtistsQuery
    /// </summary>
    public class ArtistsQuery : ItemsByNameQuery
    {
        /// <summary>
        /// Filter by artists that are on tour, or not
        /// </summary>
        /// <value><c>null</c> if [is on tour] contains no value, <c>true</c> if [is on tour]; otherwise, <c>false</c>.</value>
        public bool? IsOnTour { get; set; }
    }
}
