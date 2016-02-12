using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Dto;

namespace MediaBrowser.Server.Implementations.LiveTv.EmbyTV
{
    public interface IRecorder
    {
        Task Record(MediaSourceInfo mediaSource, string targetFile, Action onStarted, CancellationToken cancellationToken);
    }
}
