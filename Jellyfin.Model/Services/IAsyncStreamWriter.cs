using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Model.Services
{
    public interface IAsyncStreamWriter
    {
        Task WriteToAsync(Stream responseStream, CancellationToken cancellationToken);
    }
}
