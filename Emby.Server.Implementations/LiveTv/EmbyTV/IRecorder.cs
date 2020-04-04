#pragma warning disable CS1591

using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;

namespace Emby.Server.Implementations.LiveTv.EmbyTV
{
    public interface IRecorder
    {
        /// <summary>
        /// Records the specified media source.
        /// </summary>
        Task Record(IDirectStreamProvider directStreamProvider, MediaSourceInfo mediaSource, string targetFile, TimeSpan duration, Action onStarted, CancellationToken cancellationToken);

        string GetOutputPath(MediaSourceInfo mediaSource, string targetFile);
    }
}
