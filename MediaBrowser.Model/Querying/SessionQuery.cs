
namespace MediaBrowser.Model.Querying
{
    /// <summary>
    /// Class SessionQuery
    /// </summary>
    public class SessionQuery
    {
        /// <summary>
        /// Filter by sessions that are allowed to be controlled by a given user
        /// </summary>
        public string ControllableByUserId { get; set; }
    }
}
