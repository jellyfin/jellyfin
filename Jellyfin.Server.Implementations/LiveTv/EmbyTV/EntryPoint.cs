using System.Threading.Tasks;
using Jellyfin.Controller.Plugins;

namespace Jellyfin.Server.Implementations.LiveTv.EmbyTV
{
    public class EntryPoint : IServerEntryPoint
    {
        public Task RunAsync()
        {
            return EmbyTV.Current.Start();
        }

        public void Dispose()
        {
        }
    }
}
