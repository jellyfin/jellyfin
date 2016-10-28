using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using MediaBrowser.Model.Services;

namespace MediaBrowser.Controller.Net
{
    public class ServiceRequest : IServiceRequest
    {
        private readonly IRequest _request;

        public ServiceRequest(IRequest request)
        {
            _request = request;
        }

        public string RemoteIp
        {
            get { return _request.RemoteIp; }
        }

        public QueryParamCollection Headers
        {
            get { return _request.Headers; }
        }

        public QueryParamCollection QueryString
        {
            get { return _request.QueryString; }
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
