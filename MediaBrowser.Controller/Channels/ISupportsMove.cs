#pragma warning disable CS1591

using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Channels
{
    public interface ISupportsMove
    {
        bool CanMove(BaseItem item);

        Task MoveItem(string id, string destination, CancellationToken cancellationToken);
    }
}
