#pragma warning disable CS1591
#pragma warning disable SA1600

using System;

namespace MediaBrowser.Common.Plugins
{
    public interface IPluginAssembly
    {
        void SetAttributes(string assemblyFilePath, string dataFolderPath, Version assemblyVersion);

        void SetId(Guid assemblyId);
    }
}
