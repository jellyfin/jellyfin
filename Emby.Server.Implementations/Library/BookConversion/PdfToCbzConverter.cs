using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Library.BookConversion
{
    /// <summary>
    /// Simple PDF -> CBZ converter using external pdftoppm (Poppler) to rasterize pages and zipping them.
    /// This is intentionally a lightweight PoC implementation. It shells out to external binaries.
    /// </summary>
    public static class PdfToCbzConverter
    {
        /// <summary>
        /// Convert a PDF file into a CBZ archive.
        /// </summary>
        /// <param name="pdfPath">Full path to the source PDF.</param>
        /// <param name="cbzPath">Full path where the CBZ will be written.</param>
        /// <param name="dpi">Rasterization DPI to use for pdftoppm.</param>
        /// <param name="replaceOriginal">If true, attempt to replace the original PDF with the CBZ (PoC behavior).</param>
        /// <param name="fileSystem">IFileSystem abstraction for file ops (currently unused by PoC).</param>
        /// <param name="logger">ILogger for diagnostic messages.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if conversion succeeded; otherwise false.</returns>
        public static async Task<bool> ConvertAsync(
            string pdfPath,
            string cbzPath,
            int dpi,
            bool replaceOriginal,
            IFileSystem fileSystem,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(pdfPath) || string.IsNullOrEmpty(cbzPath))
            {
                throw new ArgumentException("Invalid paths");
            }

            if (!File.Exists(pdfPath))
            {
                logger?.LogWarning("PDF not found for conversion: {Path}", pdfPath);
                return false;
            }

            var tempDir = Path.Combine(Path.GetTempPath(), $"pdf2cbz_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);

            try
            {
                // Use pdftoppm to produce PNG pages: pdftoppm -r <dpi> -png input.pdf outprefix
                var outPrefix = Path.Combine(tempDir, "page");

                var startInfo = new ProcessStartInfo
                {
                    FileName = "pdftoppm",
                    Arguments = $"-r {dpi} -png " + Quote(pdfPath) + " " + Quote(outPrefix),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                try
                {
                    using var proc = Process.Start(startInfo)!;
                    if (proc is null)
                    {
                        logger?.LogError("Failed to start pdftoppm process");
                        return false;
                    }

                    var stderrTask = proc.StandardError.ReadToEndAsync(cancellationToken);
                    var stdoutTask = proc.StandardOutput.ReadToEndAsync(cancellationToken);

                    while (!proc.HasExited)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        await Task.Delay(200, cancellationToken).ConfigureAwait(false);
                    }

                    var stderr = await stderrTask.ConfigureAwait(false);
                    var stdout = await stdoutTask.ConfigureAwait(false);

                    if (proc.ExitCode != 0)
                    {
                        logger?.LogError("pdftoppm failed (exit {Code}): {Err}", proc.ExitCode, stderr);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Error running pdftoppm. Ensure poppler-utils pdftoppm is installed");
                    return false;
                }

                // Collect generated images (page-1.png, page-2.png, or page-001.png depending on pdftoppm version)
                var images = Directory.GetFiles(tempDir)
                    .Where(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(f => f, StringComparer.Ordinal)
                    .ToList();

                if (images.Count == 0)
                {
                    logger?.LogWarning("No images produced by pdftoppm for {Path}", pdfPath);
                    return false;
                }

                // Create CBZ (zip) with zero-padded filenames
                using (var zipToOpen = new FileStream(cbzPath, FileMode.Create))
                using (var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
                {
                    for (int i = 0; i < images.Count; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var img = images[i];
                        var entryName = (i + 1).ToString("D4", System.Globalization.CultureInfo.InvariantCulture) + Path.GetExtension(img).ToLowerInvariant();
                        var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                        using var entryStream = entry.Open();
                        using var fs = File.OpenRead(img);
                        await fs.CopyToAsync(entryStream, cancellationToken).ConfigureAwait(false);
                    }
                }

                if (replaceOriginal)
                {
                    try
                    {
                        var backup = pdfPath + ".backup." + Guid.NewGuid().ToString("N");
                        File.Move(pdfPath, backup);
                        File.Move(cbzPath, pdfPath); // replace
                        // move backup next to new file with .pdf.bak maybe
                        File.Move(backup, pdfPath + ".orig.pdf");
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "Failed replacing original PDF with CBZ for {Path}", pdfPath);
                        // leave cbz next to pdf
                        return false;
                    }
                }

                logger?.LogInformation("Converted PDF to CBZ: {Pdf} -> {Cbz}", pdfPath, cbzPath);
                return true;
            }
            finally
            {
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch
                {
                    // Ignore cleanup failures
                }
            }
        }

        private static string Quote(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return "\"\"";
            }

            return '"' + s.Replace("\"", "\\\"", StringComparison.Ordinal) + '"';
        }
    }
}
