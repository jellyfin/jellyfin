using MediaBrowser.Common.IO;
using MediaBrowser.Common.Kernel;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Api.Logging
{
    /// <summary>
    /// Class ScheduledTasksWebSocketListener
    /// </summary>
    [Export(typeof(IWebSocketListener))]
    public class LogFileWebSocketListener : BasePeriodicWebSocketListener<IKernel, IEnumerable<string>, LogFileWebSocketState>
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        protected override string Name
        {
            get { return "LogFile"; }
        }

        /// <summary>
        /// Initializes the specified kernel.
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        public override void Initialize(IKernel kernel)
        {
            base.Initialize(kernel);

            kernel.LoggerLoaded += kernel_LoggerLoaded;
        }

        /// <summary>
        /// Gets the data to send.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>IEnumerable{System.String}.</returns>
        protected override async Task<IEnumerable<string>> GetDataToSend(LogFileWebSocketState state)
        {
            if (!string.Equals(Kernel.LogFilePath, state.LastLogFilePath))
            {
                state.LastLogFilePath = Kernel.LogFilePath;
                state.StartLine = 0;
            }

            var lines = await GetLogLines(state.LastLogFilePath, state.StartLine).ConfigureAwait(false);

            state.StartLine += lines.Count;

            return lines;
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool dispose)
        {
            if (dispose)
            {
                Kernel.LoggerLoaded -= kernel_LoggerLoaded;
            }
            base.Dispose(dispose);
        }

        /// <summary>
        /// Handles the LoggerLoaded event of the kernel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        void kernel_LoggerLoaded(object sender, EventArgs e)
        {
            // Reset the startline for each connection whenever the logger reloads
            lock (ActiveConnections)
            {
                foreach (var connection in ActiveConnections)
                {
                    connection.Item4.StartLine = 0;
                }
            }
        }

        /// <summary>
        /// Gets the log lines.
        /// </summary>
        /// <param name="logFilePath">The log file path.</param>
        /// <param name="startLine">The start line.</param>
        /// <returns>Task{IEnumerable{System.String}}.</returns>
        internal static async Task<List<string>> GetLogLines(string logFilePath, int startLine)
        {
            var lines = new List<string>();

            using (var fs = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, StreamDefaults.DefaultFileStreamBufferSize, true))
            {
                using (var reader = new StreamReader(fs))
                {
                    while (!reader.EndOfStream)
                    {
                        lines.Add(await reader.ReadLineAsync().ConfigureAwait(false));
                    }
                }
            }

            if (startLine > 0)
            {
                lines = lines.Skip(startLine).ToList();
            }

            return lines;
        }
    }

    /// <summary>
    /// Class LogFileWebSocketState
    /// </summary>
    public class LogFileWebSocketState
    {
        /// <summary>
        /// Gets or sets the last log file path.
        /// </summary>
        /// <value>The last log file path.</value>
        public string LastLogFilePath { get; set; }
        /// <summary>
        /// Gets or sets the start line.
        /// </summary>
        /// <value>The start line.</value>
        public int StartLine { get; set; }
    }
}
