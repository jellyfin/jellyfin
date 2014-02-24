using MediaBrowser.Controller.Library;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Music
{
    public class SoundtrackPostScanTask : ILibraryPostScanTask
    {
        private readonly ILibraryManager _libraryManager;

        public SoundtrackPostScanTask(ILibraryManager libraryManager)
        {
            _libraryManager = libraryManager;
        }

        private readonly Task _cachedTask = Task.FromResult(true);
        public Task Run(IProgress<double> progress, CancellationToken cancellationToken)
        {
            RunInternal(progress, cancellationToken);

            return _cachedTask;
        }

        private void RunInternal(IProgress<double> progress, CancellationToken cancellationToken)
        {
            // Reimplement this when more kinds of associations are supported.

            progress.Report(100);
        }
    }
}
