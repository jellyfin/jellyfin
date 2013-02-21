using MediaBrowser.Common.Plugins;
using System;
using System.ComponentModel.Composition;

namespace MediaBrowser.Plugins.DefaultTheme
{
    [Export(typeof(BasePlugin))]
    public class Plugin : BaseTheme
    {
        public override string Name
        {
            get { return "Default Theme"; }
        }

        public override Uri LoginPageUri
        {
            get { return GeneratePackUri("Pages/LoginPage.xaml"); }
        }
    }
}
