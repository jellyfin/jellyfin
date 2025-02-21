#pragma warning disable CS1591

using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Channels
{
    public interface ISupportsDelete
    {
        bool CanDelete(BaseItem item);

        Task DeleteItem(string id, CancellationToken cancellationToken);
    }
}
