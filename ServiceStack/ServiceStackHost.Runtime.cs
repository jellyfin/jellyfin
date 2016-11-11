// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt


using MediaBrowser.Model.Services;
using ServiceStack.Support.WebHost;

namespace ServiceStack
{
    public abstract partial class ServiceStackHost
    {
        /// <summary>
        /// Applies the request filters. Returns whether or not the request has been handled 
        /// and no more processing should be done.
        /// </summary>
        /// <returns></returns>
        public virtual bool ApplyRequestFilters(IRequest req, IResponse res, object requestDto)
        {
            if (res.IsClosed) return res.IsClosed;

            //Exec all RequestFilter attributes with Priority < 0
            var attributes = FilterAttributeCache.GetRequestFilterAttributes(requestDto.GetType());
            var i = 0;
            for (; i < attributes.Length && attributes[i].Priority < 0; i++)
            {
                var attribute = attributes[i];
                attribute.RequestFilter(req, res, requestDto);
                if (res.IsClosed) return res.IsClosed;
            }

            if (res.IsClosed) return res.IsClosed;

            //Exec global filters
            foreach (var requestFilter in GlobalRequestFilters)
            {
                requestFilter(req, res, requestDto);
                if (res.IsClosed) return res.IsClosed;
            }

            //Exec remaining RequestFilter attributes with Priority >= 0
            for (; i < attributes.Length && attributes[i].Priority >= 0; i++)
            {
                var attribute = attributes[i];
                attribute.RequestFilter(req, res, requestDto);
                if (res.IsClosed) return res.IsClosed;
            }

            return res.IsClosed;
        }

        /// <summary>
        /// Applies the response filters. Returns whether or not the request has been handled 
        /// and no more processing should be done.
        /// </summary>
        /// <returns></returns>
        public virtual bool ApplyResponseFilters(IRequest req, IResponse res, object response)
        {
            if (response != null)
            {
                if (res.IsClosed) return res.IsClosed;
            }

            //Exec global filters
            foreach (var responseFilter in GlobalResponseFilters)
            {
                responseFilter(req, res, response);
                if (res.IsClosed) return res.IsClosed;
            }

            return res.IsClosed;
        }
    }

}