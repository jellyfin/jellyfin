using System;
using System.ComponentModel.Composition;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Events;
using MediaBrowser.Model.Plugins;
using MediaBrowser.TV.Entities;

namespace MediaBrowser.TV
{
    [Export(typeof(BasePlugin))]
    public class Plugin : BaseGenericPlugin<BasePluginConfiguration>
    {
        public override string Name
        {
            get { return "TV"; }
        }

        public override void Init()
        {
            Kernel.Instance.ItemController.PreBeginResolvePath += ItemController_PreBeginResolvePath;
        }

        public override void Dispose()
        {
            Kernel.Instance.ItemController.PreBeginResolvePath -= ItemController_PreBeginResolvePath;
        }

        void ItemController_PreBeginResolvePath(object sender, PreBeginResolveEventArgs e)
        {
            if (System.IO.Path.GetFileName(e.Path).Equals("metadata", StringComparison.OrdinalIgnoreCase) && e.IsDirectory)
            {
                if (e.Parent is Season || e.Parent is Series)
                {
                    e.Cancel = true;
                }
            }
        }
    }
}
