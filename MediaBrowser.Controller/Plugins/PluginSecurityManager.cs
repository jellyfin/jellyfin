using Mediabrowser.Model.Entities;
using Mediabrowser.PluginSecurity;
using MediaBrowser.Common.Kernel;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Plugins
{
    public class PluginSecurityManager : BaseManager<Kernel>
    {
        private bool? _isMBSupporter;
        private bool _isMBSupporterInitialized;
        private object _isMBSupporterSyncLock = new object();
        
        public bool IsMBSupporter
        {
            get
            {
                LazyInitializer.EnsureInitialized(ref _isMBSupporter, ref _isMBSupporterInitialized, ref _isMBSupporterSyncLock, () => GetRegistrationStatus("MBSupporter").Result.IsRegistered);
                return _isMBSupporter.Value;
            }
        }

        public PluginSecurityManager(Kernel kernel) : base(kernel)
        {
        }

        public async Task<MBRegistrationRecord> GetRegistrationStatus(string feature, string mb2Equivalent = null)
        {
            return await MBRegistration.GetRegistrationStatus(feature, mb2Equivalent).ConfigureAwait(false);
        }

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

        public string LegacyKey
        {
            get { return MBRegistration.LegacyKey; }
            set { 
                MBRegistration.LegacyKey = value;
                // And we'll need to restart to re-evaluate the status of plug-ins
                Kernel.NotifyPendingRestart();
            }
        }

        private void ResetSupporterInfo()
        {
            _isMBSupporter = null;
            _isMBSupporterInitialized = false;
        }
    }
}
