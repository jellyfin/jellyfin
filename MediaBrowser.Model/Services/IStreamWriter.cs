#pragma warning disable CS1591

using System.IO;

namespace MediaBrowser.Model.Services
{
    public interface IStreamWriter
    {
        void WriteTo(Stream responseStream);
    }
}
