using MediaBrowser.Common.IO;
using MediaBrowser.Model.Logging;
using ServiceStack.Web;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Playback.Progressive
{
    public class ProgressiveStreamWriter : IStreamWriter, IHasOptions
    {
        private string Path { get; set; }
        private ILogger Logger { get; set; }
        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// The _options
        /// </summary>
        private readonly IDictionary<string, string> _options = new Dictionary<string, string>();
        /// <summary>
        /// Gets the options.
        /// </summary>
        /// <value>The options.</value>
        public IDictionary<string, string> Options
        {
            get { return _options; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressiveStreamWriter" /> class.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="fileSystem">The file system.</param>
        public ProgressiveStreamWriter(string path, ILogger logger, IFileSystem fileSystem)
        {
            Path = path;
            Logger = logger;
            _fileSystem = fileSystem;
        }

        /// <summary>
        /// Writes to.
        /// </summary>
        /// <param name="responseStream">The response stream.</param>
        public void WriteTo(Stream responseStream)
        {
            var task = WriteToAsync(responseStream);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Writes to async.
        /// </summary>
        /// <param name="responseStream">The response stream.</param>
        /// <returns>Task.</returns>
        public async Task WriteToAsync(Stream responseStream)
        {
            try
            {
                await new ProgressiveFileCopier(_fileSystem).StreamFile(Path, responseStream).ConfigureAwait(false);
            }
            catch
            {
                Logger.Error("Error streaming media. The client has most likely disconnected or transcoding has failed.");

                throw;
            }
            finally
            {
                ApiEntryPoint.Instance.OnTranscodeEndRequest(Path, TranscodingJobType.Progressive);
            }
        }
    }

    public class ProgressiveFileCopier
    {
        private readonly IFileSystem _fileSystem;

        public ProgressiveFileCopier(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public async Task StreamFile(string path, Stream outputStream)
        {
            var eofCount = 0;
            long position = 0;

            using (var fs = _fileSystem.GetFileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, true))
            {
                while (eofCount < 15)
                {
                    await fs.CopyToAsync(outputStream).ConfigureAwait(false);

                    var fsPosition = fs.Position;

                    var bytesRead = fsPosition - position;

                    //Logger.Debug("Streamed {0} bytes from file {1}", bytesRead, path);

                    if (bytesRead == 0)
                    {
                        eofCount++;
                        await Task.Delay(100).ConfigureAwait(false);
                    }
                    else
                    {
                        eofCount = 0;
                    }

                    position = fsPosition;
                }
            }
        }
    }
}
