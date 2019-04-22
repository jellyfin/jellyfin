using System.Collections.Generic;

namespace Jellyfin.Model.Plugins
{
    public interface IHasWebPages
    {
        IEnumerable<PluginPageInfo> GetPages();
    }
}
