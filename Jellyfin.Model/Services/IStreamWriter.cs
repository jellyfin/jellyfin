using System.IO;

namespace Jellyfin.Model.Services
{
    public interface IStreamWriter
    {
        void WriteTo(Stream responseStream);
    }
}
