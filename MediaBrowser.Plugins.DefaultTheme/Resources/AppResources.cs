using System.ComponentModel.Composition;
using System.Windows;

namespace MediaBrowser.Plugins.DefaultTheme.Resources
{
    [Export(typeof(ResourceDictionary))]
    public partial class AppResources : ResourceDictionary
    {
        public AppResources()
        {
            InitializeComponent();
        }
    }
}
