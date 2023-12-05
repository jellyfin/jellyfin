#nullable disable

#pragma warning disable CA1711, CS1591

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Dto;

namespace MediaBrowser.Controller.Library
{
    public interface ILiveStream : IDisposable
    {
        int ConsumerCount { get; set; }

        string OriginalStreamId { get; set; }

        string TunerHostId { get; }

        bool EnableStreamSharing { get; }

        MediaSourceInfo MediaSource { get; set; }

        string UniqueId { get; }

        Task Open(CancellationToken openCancellationToken);

        Task Close();

        Stream GetStream();
    }
}
