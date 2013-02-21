using MediaBrowser.Model.Logging;
using System.Security;

namespace MediaBrowser.IsoMounter
{
    /// <summary>
    /// Class MyPfmFileMountUi
    /// </summary>
    public class MyPfmFileMountUi : PfmFileMountUi
    {
        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger Logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MyPfmFileMountUi" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public MyPfmFileMountUi(ILogger logger)
        {
            Logger = logger;
        }

        /// <summary>
        /// Clears the password.
        /// </summary>
        public void ClearPassword()
        {
        }

        /// <summary>
        /// Completes the specified error message.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        public void Complete(string errorMessage)
        {
            if (!string.IsNullOrEmpty(errorMessage))
            {
                Logger.Error("Complete {0}", errorMessage);
            }
        }

        /// <summary>
        /// Queries the password.
        /// </summary>
        /// <param name="prompt">The prompt.</param>
        /// <param name="count">The count.</param>
        /// <returns>SecureString.</returns>
        public SecureString QueryPassword(string prompt, int count)
        {
            return new SecureString();
        }

        /// <summary>
        /// Resumes this instance.
        /// </summary>
        public void Resume()
        {
            Logger.Debug("Resume");
        }

        /// <summary>
        /// Starts this instance.
        /// </summary>
        public void Start()
        {
            Logger.Debug("Start");
        }

        /// <summary>
        /// Statuses the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="endOfLine">if set to <c>true</c> [end of line].</param>
        public void Status(string data, bool endOfLine)
        {
            if (!string.IsNullOrEmpty(data))
            {
                Logger.Debug("Status {0}", data);
            }
        }

        /// <summary>
        /// Suspends this instance.
        /// </summary>
        public void Suspend()
        {
            Logger.Debug("Suspend");
        }
    }
}
