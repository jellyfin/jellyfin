using ServiceStack.Web;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace MediaBrowser.Controller.Net
{
    public class ServiceStackServiceRequest : IServiceRequest
    {
        private readonly IRequest _request;

        public ServiceStackServiceRequest(IRequest request)
        {
            _request = request;
        }

        public object OriginalRequest
        {
            get { return _request; }
        }

        public string RemoteIp
        {
            get { return _request.RemoteIp; }
        }

        private NameValueCollection _headers;
        public NameValueCollection Headers
        {
            get { return _headers ?? (_headers = Get(_request.Headers)); }
        }

        private NameValueCollection _query;
        public NameValueCollection QueryString
        {
            get { return _query ?? (_query = Get(_request.QueryString)); }
        }

        private NameValueCollection Get(INameValueCollection coll)
        {
            var nv = new NameValueCollection(StringComparer.OrdinalIgnoreCase);

            foreach (var key in coll.AllKeys)
            {
                nv[key] = coll[key];
            }

            return nv;
            //return coll.ToNameValueCollection();
        }

        public IDictionary<string, object> Items
        {
            get { return _request.Items; }
        }

        public void AddResponseHeader(string name, string value)
        {
            _request.Response.AddHeader(name, value);
        }
    }
}
