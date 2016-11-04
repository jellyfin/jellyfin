using System;
using System.IO;

namespace MediaBrowser.Model.Reflection
{
    public interface IAssemblyInfo
    {
        Stream GetManifestResourceStream(Type type, string resource);
        string[] GetManifestResourceNames(Type type);
    }
}
