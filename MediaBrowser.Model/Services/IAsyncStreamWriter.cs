#pragma warning disable CS1591
#pragma warning disable SA1600

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Model.Services
{
    public interface IAsyncStreamWriter
    {
        Task WriteToAsync(Stream responseStream, CancellationToken cancellationToken);
    }
}
