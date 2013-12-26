using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using System.Threading;

namespace MediaBrowser.Server.Implementations.EntryPoints
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
            #if __MonoCS__
            try
            {
                await _userManager.RefreshUsersMetadata(CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
                System.Console.WriteLine("RefreshUsersMetadata task error: No users? First time running?");
            }
            #else
            await _userManager.RefreshUsersMetadata(CancellationToken.None).ConfigureAwait(false);
            #endif
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
        }
    }
}
