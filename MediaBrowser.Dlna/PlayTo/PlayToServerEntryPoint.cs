using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Dlna.PlayTo
{
    public class PlayToServerEntryPoint : IServerEntryPoint
    {
        const string DefaultUser = "Play To";

        private bool _disposed;
        
        private readonly IUserManager _userManager;        
        private readonly PlayToManager _manager;

        public PlayToServerEntryPoint(ILogManager logManager, ISessionManager sessionManager, IUserManager userManager, IHttpClient httpClient, INetworkManager networkManager, IItemRepository itemRepository, ILibraryManager libraryManager)
        {
            _userManager = userManager;

            _manager = new PlayToManager(logManager.GetLogger("PlayTo"), sessionManager, httpClient, itemRepository, libraryManager, networkManager);
        }

        /// <summary>
        /// Creates the defaultuser if needed.
        /// </summary>
        private async Task<User> CreateUserIfNeeded()
        {
            var user = _userManager.Users.FirstOrDefault(u => u.Name == DefaultUser);

            if (user == null)
            {
                user = await _userManager.CreateUser(DefaultUser);

                user.Configuration.IsHidden = true;
                user.Configuration.IsAdministrator = false;
                user.SaveConfiguration();
            }

            return user;
        }

        public async void Run()
        {
            //var defaultUser = await CreateUserIfNeeded().ConfigureAwait(false);

            //_manager.Start(defaultUser);            
        }
     
        #region Dispose

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _manager.Stop();
                _manager.Dispose();
            }
        }

        #endregion
    }
}
