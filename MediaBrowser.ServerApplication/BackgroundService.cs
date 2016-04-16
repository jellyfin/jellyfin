using MediaBrowser.Model.Logging;
using System.ServiceProcess;

namespace MediaBrowser.ServerApplication
{
    /// <summary>
    /// Class BackgroundService
    /// </summary>
    public class BackgroundService : ServiceBase
    {
        public static string Name = "Emby";
        public static string DisplayName = "Emby Server";

        public static string GetExistingServiceName()
        {
            return Name;
        }

        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundService"/> class.
        /// </summary>
        public BackgroundService(ILogger logger)
        {
            _logger = logger;

            CanPauseAndContinue = false;

            CanStop = true;

            ServiceName = GetExistingServiceName();
        }

        /// <summary>
        /// When implemented in a derived class, executes when a Stop command is sent to the service by the Service Control Manager (SCM). Specifies actions to take when a service stops running.
        /// </summary>
        protected override void OnStop()
        {
            _logger.Info("Stop command received");

            base.OnStop();
        }
    }
}
