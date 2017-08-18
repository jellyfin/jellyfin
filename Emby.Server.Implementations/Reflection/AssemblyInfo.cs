using System;
using System.IO;
using System.Reflection;
using MediaBrowser.Model.Reflection;

namespace Emby.Server.Implementations.Reflection
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
