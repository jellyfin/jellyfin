#pragma warning disable CS1591
#pragma warning disable SA1600

using System.Collections.Generic;

namespace MediaBrowser.Model.Plugins
{
    public interface IHasWebPages
    {
        IEnumerable<PluginPageInfo> GetPages();
    }
}
