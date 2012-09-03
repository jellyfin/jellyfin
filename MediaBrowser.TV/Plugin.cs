using System;
using System.ComponentModel.Composition;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
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

        protected override void InitializeInternal()
        {
            Kernel.Instance.ItemController.PreBeginResolvePath += ItemController_PreBeginResolvePath;
        }

        public override void Dispose()
        {
            Kernel.Instance.ItemController.PreBeginResolvePath -= ItemController_PreBeginResolvePath;
        }

        void ItemController_PreBeginResolvePath(object sender, PreBeginResolveEventArgs e)
        {
            // Don't try and resolve files with the metadata folder
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
