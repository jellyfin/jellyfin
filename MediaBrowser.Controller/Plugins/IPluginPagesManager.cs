using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Plugins
{
    public interface IPluginPagesManager
    {
        void RegisterPluginPage(PluginPage page);

        IEnumerable<PluginPage> GetPages();
    }

    public class PluginPage
    {
        public string Id { get; set; }

        public string Url { get; set; }

        public string DisplayText { get; set; }

        public string Icon { get; set; }
    }
}
