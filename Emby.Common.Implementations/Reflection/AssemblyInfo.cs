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
#if NET46
            return type.Assembly.GetManifestResourceStream(resource);
#endif
            return type.GetTypeInfo().Assembly.GetManifestResourceStream(resource);
        }
    }
}
