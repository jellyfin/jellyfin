using MediaBrowser.Controller.Plugins;

namespace Emby.Server.Implementations.LiveTv.EmbyTV
{
    public class EntryPoint : IServerEntryPoint
    {
        public void Run()
        {
            EmbyTV.Current.Start();
        }

        public void Dispose()
        {
        }
    }
}
