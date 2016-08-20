using MediaBrowser.Controller.FileOrganization;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.FileOrganization;
using MediaBrowser.Controller.Session;
using System.Threading;

namespace MediaBrowser.Server.Implementations.FileOrganization
{
    /// <summary>
    /// Class SessionInfoWebSocketListener
    /// </summary>
    class FileOrganizationNotifier : IServerEntryPoint
    {
        private readonly IFileOrganizationService _organizationService;
        private readonly ISessionManager _sessionManager;

        public FileOrganizationNotifier(ILogger logger, IFileOrganizationService organizationService, ISessionManager sessionManager)
        {
            _organizationService = organizationService;
            _sessionManager = sessionManager;
        }

        public void Run()
        {
            _organizationService.ItemAdded += _organizationService_ItemAdded;
            _organizationService.ItemRemoved += _organizationService_ItemRemoved;
            _organizationService.ItemUpdated += _organizationService_ItemUpdated;
            _organizationService.LogReset += _organizationService_LogReset;
        }

        private void _organizationService_LogReset(object sender, EventArgs e)
        {
            _sessionManager.SendMessageToAdminSessions("AutoOrganizeUpdate", (FileOrganizationResult)null, CancellationToken.None);
        }

        private void _organizationService_ItemUpdated(object sender, GenericEventArgs<FileOrganizationResult> e)
        {
            _sessionManager.SendMessageToAdminSessions("AutoOrganizeUpdate", e.Argument, CancellationToken.None);
        }

        private void _organizationService_ItemRemoved(object sender, GenericEventArgs<FileOrganizationResult> e)
        {
            _sessionManager.SendMessageToAdminSessions("AutoOrganizeUpdate", (FileOrganizationResult)null, CancellationToken.None);
        }

        private void _organizationService_ItemAdded(object sender, GenericEventArgs<FileOrganizationResult> e)
        {
            _sessionManager.SendMessageToAdminSessions("AutoOrganizeUpdate", (FileOrganizationResult)null, CancellationToken.None);
        }

        public void Dispose()
        {
            _organizationService.ItemAdded -= _organizationService_ItemAdded;
            _organizationService.ItemRemoved -= _organizationService_ItemRemoved;
            _organizationService.ItemUpdated -= _organizationService_ItemUpdated;
            _organizationService.LogReset -= _organizationService_LogReset;
        }


    }
}
