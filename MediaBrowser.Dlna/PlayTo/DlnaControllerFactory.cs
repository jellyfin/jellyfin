using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.Dlna.PlayTo
{
    public class PlayToControllerFactory : ISessionControllerFactory
    {
        private readonly ISessionManager _sessionManager;
        private readonly IItemRepository _itemRepository;
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;
        private readonly INetworkManager _networkManager;

        public PlayToControllerFactory(ISessionManager sessionManager, IItemRepository itemRepository, ILibraryManager libraryManager, ILogManager logManager, INetworkManager networkManager)
        {
            _itemRepository = itemRepository;
            _sessionManager = sessionManager;
            _libraryManager = libraryManager;
            _networkManager = networkManager;
            _logger = logManager.GetLogger("PlayTo");
        }

        public ISessionController GetSessionController(SessionInfo session)
        {
            return null;
        }
    }
}
