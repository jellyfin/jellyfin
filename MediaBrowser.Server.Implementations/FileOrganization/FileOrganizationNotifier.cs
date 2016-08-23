using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller.FileOrganization;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.FileOrganization;
using MediaBrowser.Model.Logging;
using System;
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
        private readonly ITaskManager _taskManager;

        public FileOrganizationNotifier(ILogger logger, IFileOrganizationService organizationService, ISessionManager sessionManager, ITaskManager taskManager)
        {
            _organizationService = organizationService;
            _sessionManager = sessionManager;
            _taskManager = taskManager;
        }

        public void Run()
        {
            _organizationService.ItemAdded += _organizationService_ItemAdded;
            _organizationService.ItemRemoved += _organizationService_ItemRemoved;
            _organizationService.ItemUpdated += _organizationService_ItemUpdated;
            _organizationService.LogReset += _organizationService_LogReset;

            //_taskManager.TaskCompleted += _taskManager_TaskCompleted;
        }

        private void _organizationService_LogReset(object sender, EventArgs e)
        {
            _sessionManager.SendMessageToAdminSessions("AutoOrganize_LogReset", (FileOrganizationResult)null, CancellationToken.None);
        }

        private void _organizationService_ItemUpdated(object sender, GenericEventArgs<FileOrganizationResult> e)
        {
            _sessionManager.SendMessageToAdminSessions("AutoOrganize_ItemUpdated", e.Argument, CancellationToken.None);
        }

        private void _organizationService_ItemRemoved(object sender, GenericEventArgs<FileOrganizationResult> e)
        {
            _sessionManager.SendMessageToAdminSessions("AutoOrganize_ItemRemoved", e.Argument, CancellationToken.None);
        }

        private void _organizationService_ItemAdded(object sender, GenericEventArgs<FileOrganizationResult> e)
        {
            _sessionManager.SendMessageToAdminSessions("AutoOrganize_ItemAdded", e.Argument, CancellationToken.None);
        }

        //private void _taskManager_TaskCompleted(object sender, TaskCompletionEventArgs e)
        //{
        //    var taskWithKey = e.Task.ScheduledTask as IHasKey;
        //    if (taskWithKey != null && taskWithKey.Key == "AutoOrganize")
        //    {
        //        _sessionManager.SendMessageToAdminSessions("AutoOrganize_TaskCompleted", (FileOrganizationResult)null, CancellationToken.None);
        //    }
        //}

        public void Dispose()
        {
            _organizationService.ItemAdded -= _organizationService_ItemAdded;
            _organizationService.ItemRemoved -= _organizationService_ItemRemoved;
            _organizationService.ItemUpdated -= _organizationService_ItemUpdated;
            _organizationService.LogReset -= _organizationService_LogReset;

            //_taskManager.TaskCompleted -= _taskManager_TaskCompleted;
        }


    }
}
