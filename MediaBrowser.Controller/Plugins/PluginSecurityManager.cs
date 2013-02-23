using Mediabrowser.Model.Entities;
using Mediabrowser.PluginSecurity;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Plugins
{
    /// <summary>
    /// Class PluginSecurityManager
    /// </summary>
    public class PluginSecurityManager : BaseManager<Kernel>
    {
        /// <summary>
        /// The _is MB supporter
        /// </summary>
        private bool? _isMBSupporter;
        /// <summary>
        /// The _is MB supporter initialized
        /// </summary>
        private bool _isMBSupporterInitialized;
        /// <summary>
        /// The _is MB supporter sync lock
        /// </summary>
        private object _isMBSupporterSyncLock = new object();

        /// <summary>
        /// Gets a value indicating whether this instance is MB supporter.
        /// </summary>
        /// <value><c>true</c> if this instance is MB supporter; otherwise, <c>false</c>.</value>
        public bool IsMBSupporter
        {
            get
            {
                LazyInitializer.EnsureInitialized(ref _isMBSupporter, ref _isMBSupporterInitialized, ref _isMBSupporterSyncLock, () => GetRegistrationStatus("MBSupporter").Result.IsRegistered);
                return _isMBSupporter.Value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginSecurityManager" /> class.
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        public PluginSecurityManager(Kernel kernel) : base(kernel)
        {
        }

        /// <summary>
        /// Gets the registration status.
        /// </summary>
        /// <param name="feature">The feature.</param>
        /// <param name="mb2Equivalent">The MB2 equivalent.</param>
        /// <returns>Task{MBRegistrationRecord}.</returns>
        public async Task<MBRegistrationRecord> GetRegistrationStatus(string feature, string mb2Equivalent = null)
        {
            return await MBRegistration.GetRegistrationStatus(feature, mb2Equivalent).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets or sets the supporter key.
        /// </summary>
        /// <value>The supporter key.</value>
        public string SupporterKey
        {
            get { return MBRegistration.SupporterKey; }
            set {
                if (value != MBRegistration.SupporterKey)
                {
                    MBRegistration.SupporterKey = value;
                    // Clear this so it will re-evaluate
                    ResetSupporterInfo();
                    // And we'll need to restart to re-evaluate the status of plug-ins
                    Kernel.NotifyPendingRestart();
                    
                }
            }
        }

        /// <summary>
        /// Gets or sets the legacy key.
        /// </summary>
        /// <value>The legacy key.</value>
        public string LegacyKey
        {
            get { return MBRegistration.LegacyKey; }
            set { 
                MBRegistration.LegacyKey = value;
                // And we'll need to restart to re-evaluate the status of plug-ins
                Kernel.NotifyPendingRestart();
            }
        }

        /// <summary>
        /// Resets the supporter info.
        /// </summary>
        private void ResetSupporterInfo()
        {
            _isMBSupporter = null;
            _isMBSupporterInitialized = false;
        }
    }
}
