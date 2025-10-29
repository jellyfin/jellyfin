using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Library.BookConversion
{
    /// <summary>
    /// Post-scan task that finds Book items backed by PDFs and queues conversion tasks.
    /// Conversion is controlled by ServerConfiguration.EnablePdfToCbzConversion (disabled by default).
    /// </summary>
    public class PdfConversionPostScanTask : ILibraryPostScanTask
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ITaskManager _taskManager;
        private readonly IServerConfigurationManager _configManager;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<PdfConversionPostScanTask> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PdfConversionPostScanTask"/> class.
        /// </summary>
        /// <param name="libraryManager">Library manager.</param>
        /// <param name="taskManager">Task manager.</param>
        /// <param name="configManager">Server configuration manager.</param>
        /// <param name="fileSystem">File system abstraction.</param>
        /// <param name="logger">Logger.</param>
        public PdfConversionPostScanTask(
            ILibraryManager libraryManager,
            ITaskManager taskManager,
            IServerConfigurationManager configManager,
            IFileSystem fileSystem,
            ILogger<PdfConversionPostScanTask> logger)
        {
            _libraryManager = libraryManager;
            _taskManager = taskManager;
            _configManager = configManager;
            _fileSystem = fileSystem;
            _logger = logger;
        }

    /// <summary>
        /// Runs the post-scan job which queues PDF->CBZ conversions for detected PDF book files.
        /// </summary>
        /// <param name="progress">Progress reporter.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A completed task when queuing is finished.</returns>
        public Task Run(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var cfg = _configManager.Configuration;
            if (!cfg.EnablePdfToCbzConversion)
            {
                _logger.LogDebug("PDF->CBZ conversion disabled in server configuration.");
                return Task.CompletedTask;
            }

            // Query all Book items and schedule conversion for those that appear to be PDFs
            var query = new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.Book },
                Recursive = true,
                DtoOptions = new MediaBrowser.Controller.Dto.DtoOptions(false) { EnableImages = false }
            };

            var items = _libraryManager.GetItemList(query);

            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrEmpty(item.Path))
                {
                    continue;
                }

                if (!item.Path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    var dpi = cfg.PdfToCbzDpi > 0 ? cfg.PdfToCbzDpi : 150;
                    var replace = cfg.PdfToCbzReplaceOriginal;

                    var task = new PdfToCbzConversionTask(item.Path, dpi, replace, _fileSystem, _logger);

                    _taskManager.QueueScheduledTask(task, new TaskOptions());
                    _logger.LogInformation("Queued PDF->CBZ conversion for {Path}", item.Path);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to queue conversion for {Path}", item.Path);
                }
            }

            progress.Report(100);
            return Task.CompletedTask;
        }
    }
}
