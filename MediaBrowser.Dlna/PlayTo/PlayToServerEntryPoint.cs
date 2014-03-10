using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Logging;
using System;

namespace MediaBrowser.Dlna.PlayTo
{
    public class PlayToServerEntryPoint : IServerEntryPoint
    {
        private  PlayToManager _manager;
        private readonly IServerConfigurationManager _config;
        private readonly ILogger _logger;
        private readonly ISessionManager _sessionManager;
        private readonly IHttpClient _httpClient;
        private readonly IItemRepository _itemRepo;
        private readonly ILibraryManager _libraryManager;
        private readonly INetworkManager _networkManager;
        private readonly IUserManager _userManager;

        public PlayToServerEntryPoint(ILogManager logManager, IServerConfigurationManager config, ISessionManager sessionManager, IHttpClient httpClient, IItemRepository itemRepo, ILibraryManager libraryManager, INetworkManager networkManager, IUserManager userManager)
        {
            _config = config;
            _sessionManager = sessionManager;
            _httpClient = httpClient;
            _itemRepo = itemRepo;
            _libraryManager = libraryManager;
            _networkManager = networkManager;
            _userManager = userManager;
            _logger = logManager.GetLogger("PlayTo");
        }

        public void Run()
        {
            _config.ConfigurationUpdated += ConfigurationUpdated;
            ReloadPlayToManager();
        }

        void ConfigurationUpdated(object sender, EventArgs e)
        {
            ReloadPlayToManager();
        }

        private void ReloadPlayToManager()
        {
            var isStarted = _manager != null;

            if (_config.Configuration.DlnaOptions.EnablePlayTo && !isStarted)
            {
                StartPlayToManager();
            }
            else if (!_config.Configuration.DlnaOptions.EnablePlayTo && isStarted)
            {
                DisposePlayToManager();
            }
        }

        private readonly object _syncLock = new object();
        private void StartPlayToManager()
        {
            lock (_syncLock)
            {
                try
                {
                    _manager = new PlayToManager(_logger, _sessionManager, _httpClient, _itemRepo, _libraryManager, _networkManager, _userManager);
                    _manager.Start();
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error starting PlayTo manager", ex);
                }
            }
        }

        private void DisposePlayToManager()
        {
            lock (_syncLock)
            {
                if (_manager != null)
                {
                    try
                    {
                        _manager.Stop();
                        _manager.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error disposing PlayTo manager", ex);
                    }
                    _manager = null;
                }
            }
        }

        #region Dispose

        public void Dispose()
        {
            DisposePlayToManager();
        }

        #endregion
    }
}
