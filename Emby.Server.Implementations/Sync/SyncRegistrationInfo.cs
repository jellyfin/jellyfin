using MediaBrowser.Common.Security;
using System.Threading.Tasks;

namespace Emby.Server.Implementations.Sync
{
    public class SyncRegistrationInfo : IRequiresRegistration
    {
        private readonly ISecurityManager _securityManager;

        public static SyncRegistrationInfo Instance;

        public SyncRegistrationInfo(ISecurityManager securityManager)
        {
            _securityManager = securityManager;
            Instance = this;
        }

        private bool _registered;
        public bool IsRegistered
        {
            get { return _registered; }
        }

        public async Task LoadRegistrationInfoAsync()
        {
            var info = await _securityManager.GetRegistrationStatus("sync").ConfigureAwait(false);

            _registered = info.IsValid;
        }
    }
}
