using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Common.Extensions;

namespace TestPlugin.Entities
{
    [Export(typeof(BasePluginFolder))]
    public class MyPluginFolder : BasePluginFolder
    {
        public MyPluginFolder()
        {
            Name = "Test Plug-in Folder";
            Id = (GetType().FullName + Name.ToLower()).GetMD5();
        }

        public override bool IsVisible(User user)
        {
            return base.IsVisible(user) && user.Name.Equals("Abobader", StringComparison.OrdinalIgnoreCase);
        }
    }
}
