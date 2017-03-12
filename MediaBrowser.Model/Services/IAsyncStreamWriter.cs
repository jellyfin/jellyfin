using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Model.Services
{
    public interface IAsyncStreamWriter
    {
        Task WriteToAsync(Stream responseStream, CancellationToken cancellationToken);
    }

    public interface IFileWriter
    {
        Task WriteToAsync(Stream responseStream, CancellationToken cancellationToken);
    }
}
