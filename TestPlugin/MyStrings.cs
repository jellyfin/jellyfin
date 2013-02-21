using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using MediaBrowser.Common.Localization;

namespace TestPlugin
{
    [Export(typeof(LocalizedStringData))]
    public class MyStrings : LocalizedStringData
    {
        public MyStrings() : base()
        {
            ThisVersion = "1.0001";
            Prefix = "TestPlugin-";
        }

        public string TestPluginString1 = "This is string 1";
        public string TestPluginString2 = "This is string 2";
        public string TestPluginString3 = "This is string 3";
    }
}
