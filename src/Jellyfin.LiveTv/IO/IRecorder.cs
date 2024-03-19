#pragma warning disable CS1591

using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;

namespace Jellyfin.LiveTv.IO
{
    public interface IRecorder : IDisposable
    {
        /// <summary>
        /// Records the specified media source.
        /// </summary>
        /// <param name="directStreamProvider">The direct stream provider, or <c>null</c>.</param>
        /// <param name="mediaSource">The media source.</param>
        /// <param name="targetFile">The target file.</param>
        /// <param name="duration">The duration to record.</param>
        /// <param name="onStarted">An action to perform when recording starts.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> that represents the recording operation.</returns>
        Task Record(IDirectStreamProvider? directStreamProvider, MediaSourceInfo mediaSource, string targetFile, TimeSpan duration, Action onStarted, CancellationToken cancellationToken);

        string GetOutputPath(MediaSourceInfo mediaSource, string targetFile);
    }
}
