using System;
using System.IO;
using MediaBrowser.Model.Reflection;

namespace MediaBrowser.Server.Implementations.Reflection
{
    public class AssemblyInfo : IAssemblyInfo
    {
        public Stream GetManifestResourceStream(Type type, string resource)
        {
            return type.Assembly.GetManifestResourceStream(resource);
        }
    }
}
