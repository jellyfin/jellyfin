using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Dto;

namespace MediaBrowser.Server.Implementations.LiveTv.EmbyTV
{
    public interface IRecorder
    {
        /// <summary>
        /// Records the specified media source.
        /// </summary>
        /// <param name="mediaSource">The media source.</param>
        /// <param name="targetFile">The target file.</param>
        /// <param name="duration">The duration.</param>
        /// <param name="onStarted">The on started.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task Record(MediaSourceInfo mediaSource, string targetFile, TimeSpan duration, Action onStarted, CancellationToken cancellationToken);

        string GetOutputPath(MediaSourceInfo mediaSource, string targetFile);
    }
}
