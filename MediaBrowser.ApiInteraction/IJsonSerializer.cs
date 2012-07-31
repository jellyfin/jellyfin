using System;
using System.IO;

namespace MediaBrowser.ApiInteraction
{
    /// <summary>
    /// Since ServiceStack Json is not portable, we need to abstract required json functions into an interface
    /// </summary>
    public interface IJsonSerializer
    {
        T DeserializeFromStream<T>(Stream stream);
    }
}
