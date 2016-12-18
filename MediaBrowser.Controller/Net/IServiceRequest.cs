using System.Collections.Generic;
using MediaBrowser.Model.Services;

namespace MediaBrowser.Controller.Net
{
    public interface IServiceRequest
    {
        string RemoteIp { get; }
        QueryParamCollection Headers { get; }
        QueryParamCollection QueryString { get; }
        IDictionary<string,object> Items { get; }
        void AddResponseHeader(string name, string value);
    }
}
