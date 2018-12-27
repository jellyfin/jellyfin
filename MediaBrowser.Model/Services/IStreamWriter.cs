using System.IO;

namespace MediaBrowser.Model.Services
{
    public interface IStreamWriter
    {
        void WriteTo(Stream responseStream);
    }
}
