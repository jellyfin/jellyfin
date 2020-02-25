#pragma warning disable CS1591
#pragma warning disable SA1600

using System.Threading.Tasks;
using MediaBrowser.Controller.Plugins;

namespace Emby.Server.Implementations.LiveTv.EmbyTV
{
    public class EntryPoint : IServerEntryPoint
    {
        /// <inheritdoc />
        public Task RunAsync()
        {
            return EmbyTV.Current.Start();
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}
