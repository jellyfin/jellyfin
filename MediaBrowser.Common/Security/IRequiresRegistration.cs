using System.Threading.Tasks;

namespace MediaBrowser.Common.Security
{
    public interface IRequiresRegistration
    {
        /// <summary>
        /// Load all registration information required for this entity.
        /// Your class should re-load all MBRegistrationRecords when this is called even if they were
        /// previously loaded.
        /// </summary>
        /// <returns></returns>
        Task LoadRegistrationInfoAsync();
    }
}
