using System;
using System.IO;
using System.Threading.Tasks;

namespace MediaBrowser.ApiInteraction
{
    public interface IHttpClient : IDisposable  
    {
        Task<Stream> GetStreamAsync(string url);
    }
}
