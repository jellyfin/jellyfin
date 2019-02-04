using System.Threading.Tasks;
using MediaBrowser.Controller.Plugins;

namespace Emby.Server.Implementations.LiveTv.EmbyTV
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
