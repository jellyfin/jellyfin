using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.Dlna.PlayTo
{
    public class PlayToServerEntryPoint : IServerEntryPoint
    {
        private bool _disposed;

        private readonly PlayToManager _manager;

        public PlayToServerEntryPoint(ILogManager logManager, ISessionManager sessionManager, IUserManager userManager, IHttpClient httpClient, INetworkManager networkManager, IItemRepository itemRepository, ILibraryManager libraryManager)
        {
            _manager = new PlayToManager(logManager.GetLogger("PlayTo"), sessionManager, httpClient, itemRepository, libraryManager, networkManager, userManager);
        }

        public void Run()
        {
            //_manager.Start();            
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
