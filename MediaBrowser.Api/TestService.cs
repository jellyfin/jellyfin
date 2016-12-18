using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Model.Services;

namespace MediaBrowser.Api
{
    [Route("/Test/String", "GET")]
    public class GetString
    {
    }

    [Route("/Test/OptimizedString", "GET")]
    public class GetOptimizedString
    {
    }

    [Route("/Test/Bytes", "GET")]
    public class GetBytes
    {
    }

    [Route("/Test/OptimizedBytes", "GET")]
    public class GetOptimizedBytes
    {
    }

    [Route("/Test/Stream", "GET")]
    public class GetStream
    {
    }

    [Route("/Test/OptimizedStream", "GET")]
    public class GetOptimizedStream
    {
    }

    [Route("/Test/BytesWithContentType", "GET")]
    public class GetBytesWithContentType
    {
    }

    public class TestService : BaseApiService
    {
        public object Get(GetString request)
        {
            return "Welcome to Emby!";
        }
        public object Get(GetOptimizedString request)
        {
            return ToOptimizedResult("Welcome to Emby!");
        }
        public object Get(GetBytes request)
        {
            return Encoding.UTF8.GetBytes("Welcome to Emby!");
        }
        public object Get(GetOptimizedBytes request)
        {
            return ToOptimizedResult(Encoding.UTF8.GetBytes("Welcome to Emby!"));
        }
        public object Get(GetBytesWithContentType request)
        {
            return ApiEntryPoint.Instance.ResultFactory.GetResult(Encoding.UTF8.GetBytes("Welcome to Emby!"), "text/html");
        }
        public object Get(GetStream request)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes("Welcome to Emby!"));
        }
        public object Get(GetOptimizedStream request)
        {
            return ToOptimizedResult(new MemoryStream(Encoding.UTF8.GetBytes("Welcome to Emby!")));
        }
    }
}
