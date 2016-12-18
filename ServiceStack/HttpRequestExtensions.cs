using System;
using System.Collections.Generic;
using MediaBrowser.Model.Services;
using ServiceStack.Host;

namespace ServiceStack
{
    public static class HttpRequestExtensions
    {
        /**
         * 
             Input: http://localhost:96/Cambia3/Temp/Test.aspx/path/info?q=item#fragment

            Some HttpRequest path and URL properties:
            Request.ApplicationPath:	/Cambia3
            Request.CurrentExecutionFilePath:	/Cambia3/Temp/Test.aspx
            Request.FilePath:			/Cambia3/Temp/Test.aspx
            Request.Path:				/Cambia3/Temp/Test.aspx/path/info
            Request.PathInfo:			/path/info
            Request.PhysicalApplicationPath:	D:\Inetpub\wwwroot\CambiaWeb\Cambia3\
            Request.QueryString:		/Cambia3/Temp/Test.aspx/path/info?query=arg
            Request.Url.AbsolutePath:	/Cambia3/Temp/Test.aspx/path/info
            Request.Url.AbsoluteUri:	http://localhost:96/Cambia3/Temp/Test.aspx/path/info?query=arg
            Request.Url.Fragment:	
            Request.Url.Host:			localhost
            Request.Url.LocalPath:		/Cambia3/Temp/Test.aspx/path/info
            Request.Url.PathAndQuery:	/Cambia3/Temp/Test.aspx/path/info?query=arg
            Request.Url.Port:			96
            Request.Url.Query:			?query=arg
            Request.Url.Scheme:			http
            Request.Url.Segments:		/
                                        Cambia3/
                                        Temp/
                                        Test.aspx/
                                        path/
                                        info
         * */

        /// <summary>
        /// Duplicate Params are given a unique key by appending a #1 suffix
        /// </summary>
        public static Dictionary<string, string> GetRequestParams(this IRequest request)
        {
            var map = new Dictionary<string, string>();

            foreach (var name in request.QueryString.Keys)
            {
                if (name == null) continue; //thank you ASP.NET

                var values = request.QueryString.GetValues(name);
                if (values.Length == 1)
                {
                    map[name] = values[0];
                }
                else
                {
                    for (var i = 0; i < values.Length; i++)
                    {
                        map[name + (i == 0 ? "" : "#" + i)] = values[i];
                    }
                }
            }

            if ((request.Verb == HttpMethods.Post || request.Verb == HttpMethods.Put)
                && request.FormData != null)
            {
                foreach (var name in request.FormData.Keys)
                {
                    if (name == null) continue; //thank you ASP.NET

                    var values = request.FormData.GetValues(name);
                    if (values.Length == 1)
                    {
                        map[name] = values[0];
                    }
                    else
                    {
                        for (var i = 0; i < values.Length; i++)
                        {
                            map[name + (i == 0 ? "" : "#" + i)] = values[i];
                        }
                    }
                }
            }

            return map;
        }

        /// <summary>
        /// Duplicate params have their values joined together in a comma-delimited string
        /// </summary>
        public static Dictionary<string, string> GetFlattenedRequestParams(this IRequest request)
        {
            var map = new Dictionary<string, string>();

            foreach (var name in request.QueryString.Keys)
            {
                if (name == null) continue; //thank you ASP.NET
                map[name] = request.QueryString[name];
            }

            if ((request.Verb == HttpMethods.Post || request.Verb == HttpMethods.Put)
                && request.FormData != null)
            {
                foreach (var name in request.FormData.Keys)
                {
                    if (name == null) continue; //thank you ASP.NET
                    map[name] = request.FormData[name];
                }
            }

            return map;
        }

        public static void SetRoute(this IRequest req, RestPath route)
        {
            req.Items["__route"] = route;
        }

        public static RestPath GetRoute(this IRequest req)
        {
            object route;
            req.Items.TryGetValue("__route", out route);
            return route as RestPath;
        }
    }
}