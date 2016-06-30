using System.Threading.Tasks;
using MediaBrowser.Common.Security;

namespace MediaBrowser.Server.Implementations.LiveTv.EmbyTV
{
    public class EmbyTVRegistration : IRequiresRegistration
    {
        private readonly ISecurityManager _securityManager;

        public static EmbyTVRegistration Instance;

        public EmbyTVRegistration(ISecurityManager securityManager)
        {
            _securityManager = securityManager;
            Instance = this;
        }

        private bool? _isXmlTvEnabled;

        public Task LoadRegistrationInfoAsync()
        {
            _isXmlTvEnabled = null;
            return Task.FromResult(true);
        }

        public async Task<bool> EnableXmlTv()
        {
            if (!_isXmlTvEnabled.HasValue)
            {
                var info = await _securityManager.GetRegistrationStatus("xmltv").ConfigureAwait(false);
                _isXmlTvEnabled = info.IsValid;
            }
            return _isXmlTvEnabled.Value;
        }
    }
}
