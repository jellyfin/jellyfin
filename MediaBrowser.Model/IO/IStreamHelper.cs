#pragma warning disable CS1591

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Model.IO
{
    public interface IStreamHelper
    {
        Task CopyToAsync(Stream source, Stream destination, int bufferSize, Action onStarted, CancellationToken cancellationToken);

        Task CopyToAsync(Stream source, Stream destination, int bufferSize, int emptyReadLimit, CancellationToken cancellationToken);

        Task<int> CopyToAsync(Stream source, Stream destination, CancellationToken cancellationToken);

        Task CopyToAsync(Stream source, Stream destination, long copyLength, CancellationToken cancellationToken);

        Task CopyUntilCancelled(Stream source, Stream target, int bufferSize, CancellationToken cancellationToken);
    }
}
