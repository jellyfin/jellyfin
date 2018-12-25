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
        Task<bool> IsSupporter();

        /// <summary>
        /// Gets or sets the supporter key.
        /// </summary>
        /// <value>The supporter key.</value>
        string SupporterKey { get; }

        /// <summary>
        /// Gets the registration status. Overload to support existing plug-ins.
        /// </summary>
        Task<MBRegistrationRecord> GetRegistrationStatus(string feature);

        /// <summary>
        /// Register and app store sale with our back-end
        /// </summary>
        /// <param name="parameters">Json parameters to pass to admin server</param>
        Task RegisterAppStoreSale(string parameters);
        Task UpdateSupporterKey(string newValue);
    }
}