using MediaBrowser.Model.Entities;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Security
{
    public interface ISecurityManager
    {
        /// <summary>
        /// Gets a value indicating whether this instance is MB supporter.
        /// </summary>
        /// <value><c>true</c> if this instance is MB supporter; otherwise, <c>false</c>.</value>
        bool IsMBSupporter { get; }

        /// <summary>
        /// Gets or sets the supporter key.
        /// </summary>
        /// <value>The supporter key.</value>
        string SupporterKey { get; set; }

        /// <summary>
        /// Gets the registration status. Overload to support existing plug-ins.
        /// </summary>
        /// <param name="feature">The feature.</param>
        /// <param name="mb2Equivalent">The MB2 equivalent.</param>
        /// <returns>Task{MBRegistrationRecord}.</returns>
        Task<MBRegistrationRecord> GetRegistrationStatus(string feature, string mb2Equivalent = null);

        /// <summary>
        /// Gets the registration status.
        /// </summary>
        /// <param name="feature">The feature.</param>
        /// <param name="mb2Equivalent">The MB2 equivalent.</param>
        /// <param name="version">The version of the feature</param>
        /// <returns>Task{MBRegistrationRecord}.</returns>
        Task<MBRegistrationRecord> GetRegistrationStatus(string feature, string mb2Equivalent, string version);

        /// <summary>
        /// Load all registration info for all entities that require registration
        /// </summary>
        /// <returns></returns>
        Task LoadAllRegistrationInfo();

        /// <summary>
        /// Register and app store sale with our back-end
        /// </summary>
        /// <param name="parameters">Json parameters to pass to admin server</param>
        Task RegisterAppStoreSale(string parameters);
    }
}