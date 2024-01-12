#pragma warning disable CS1591

using System.Threading.Tasks;
using MediaBrowser.Controller.Plugins;

namespace Jellyfin.LiveTv.EmbyTV
{
    public sealed class EntryPoint : IServerEntryPoint
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
