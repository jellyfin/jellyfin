using System;
using System.IO;
using System.Reflection;

namespace MediaBrowser.Model.Reflection
{
    public interface IAssemblyInfo
    {
        Stream GetManifestResourceStream(Type type, string resource);
        string[] GetManifestResourceNames(Type type);

        Assembly[] GetCurrentAssemblies();
    }
}
