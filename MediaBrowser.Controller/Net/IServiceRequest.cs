using System.Collections.Generic;
using System.Collections.Specialized;

namespace MediaBrowser.Controller.Net
{
    public interface IServiceRequest
    {
        object OriginalRequest { get; }
        string RemoteIp { get; }
        NameValueCollection Headers { get; }
        NameValueCollection QueryString { get; }
        IDictionary<string,object> Items { get; }
        void AddResponseHeader(string name, string value);
    }
}
