using MediaBrowser.Model.Entities;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.LiveTv
{
    public interface IHasRegistrationInfo
    {
        /// <summary>
        /// Gets the registration information.
        /// </summary>
        /// <param name="feature">The feature.</param>
        /// <returns>Task&lt;MBRegistrationRecord&gt;.</returns>
        Task<MBRegistrationRecord> GetRegistrationInfo(string feature);
    }
}
