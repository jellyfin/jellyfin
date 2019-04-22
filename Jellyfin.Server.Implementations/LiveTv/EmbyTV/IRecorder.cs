using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Controller.Library;
using Jellyfin.Model.Dto;

namespace Jellyfin.Server.Implementations.LiveTv.EmbyTV
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
