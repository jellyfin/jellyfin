using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using System.Threading;

namespace Emby.Server.Implementations.EntryPoints
{
    /// <summary>
    /// Class RefreshUsersMetadata
    /// </summary>
    public class RefreshUsersMetadata : IServerEntryPoint
    {
        /// <summary>
        /// The _user manager
        /// </summary>
        private readonly IUserManager _userManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="RefreshUsersMetadata" /> class.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        public RefreshUsersMetadata(IUserManager userManager)
        {
            _userManager = userManager;
        }

        /// <summary>
        /// Runs this instance.
        /// </summary>
        public async void Run()
        {
            await _userManager.RefreshUsersMetadata(CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
        }
    }
}
