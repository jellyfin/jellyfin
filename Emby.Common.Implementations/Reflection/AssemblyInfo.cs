using System;
using System.IO;
using MediaBrowser.Model.Reflection;
using System.Reflection;

namespace Emby.Common.Implementations.Reflection
{
    public class AssemblyInfo : IAssemblyInfo
    {
        public Stream GetManifestResourceStream(Type type, string resource)
        {
            return type.Assembly.GetManifestResourceStream(resource);
        }

        public string[] GetManifestResourceNames(Type type)
        {
            return type.Assembly.GetManifestResourceNames();
        }

        public Assembly[] GetCurrentAssemblies()
        {
            return AppDomain.CurrentDomain.GetAssemblies();
        }
    }
}
