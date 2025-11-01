using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using MediaBrowser.Model.IO;
using MediaBrowser.Model.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Library.BookConversion
{
    /// <summary>
    /// One-off scheduled task that converts a single PDF to CBZ.
    /// Instances are created and queued by the post-scan task.
    /// </summary>
    public class PdfToCbzConversionTask : IScheduledTask
    {
        private readonly string _pdfPath;
        private readonly int _dpi;
        private readonly bool _replaceOriginal;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PdfToCbzConversionTask"/> class.
    /// </summary>
    /// <remarks>
    /// Parameterless constructor for infrastructure that may create task instances via reflection/DI.
    /// This will produce a no-op task until properties are set by the creator.
    /// </remarks>
        [ActivatorUtilitiesConstructor]
        public PdfToCbzConversionTask()
        {
            _pdfPath = string.Empty;
            _dpi = 150;
            _replaceOriginal = false;
            _fileSystem = null!;
            _logger = null!;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PdfToCbzConversionTask"/> class.
        /// </summary>
        /// <param name="pdfPath">Path to the source PDF.</param>
        /// <param name="dpi">Rasterization DPI.</param>
        /// <param name="replaceOriginal">Whether to replace the original PDF with the CBZ.</param>
        /// <param name="fileSystem">File system abstraction.</param>
        /// <param name="logger">Logger.</param>
        public PdfToCbzConversionTask(string pdfPath, int dpi, bool replaceOriginal, IFileSystem fileSystem, ILogger logger)
        {
            _pdfPath = pdfPath ?? throw new ArgumentNullException(nameof(pdfPath));
            _dpi = dpi;
            _replaceOriginal = replaceOriginal;
            _fileSystem = fileSystem;
            _logger = logger;
        }

        /// <inheritdoc />
        public string Name => "Convert PDF to CBZ";

        /// <inheritdoc />
        public string Key => "PdfToCbzConversionTask";

        /// <inheritdoc />
        public string Description => "Converts a single PDF book into a CBZ archive.";

        /// <inheritdoc />
        public string Category => "Library";

        /// <inheritdoc />
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return Array.Empty<TaskTriggerInfo>();
        }

        /// <inheritdoc />
        public async Task ExecuteAsync(System.IProgress<double> progress, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_pdfPath) || _fileSystem is null || _logger is null)
            {
                // Task was created by infrastructure without parameters — nothing to do.
                return;
            }

            var cbzPath = Path.ChangeExtension(_pdfPath, ".cbz");

            try
            {
                var success = await PdfToCbzConverter.ConvertAsync(_pdfPath, cbzPath, _dpi, _replaceOriginal, _fileSystem, _logger, cancellationToken).ConfigureAwait(false);
                if (!success)
                {
                    _logger?.LogWarning("PDF->CBZ conversion failed for {Path}", _pdfPath);
                }
            }
            catch (OperationCanceledException)
            {
                _logger?.LogInformation("PDF->CBZ conversion canceled for {Path}", _pdfPath);
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error converting PDF to CBZ for {Path}", _pdfPath);
            }

            progress.Report(100);
        }
    }
}
