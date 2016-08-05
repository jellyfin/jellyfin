using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ServiceStack;
using ServiceStack.Web;
using MediaBrowser.Controller.Net;

namespace MediaBrowser.Server.Implementations.HttpServer
{
    public class AsyncStreamWriterEx : AsyncStreamWriter, IHttpResult
    {
        /// <summary>
        /// Gets or sets the source stream.
        /// </summary>
        /// <value>The source stream.</value>
        private IAsyncStreamSource _source;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncStreamWriter" /> class.
        /// </summary>
        public AsyncStreamWriterEx(IAsyncStreamSource source) : base(source)
        {
            _source = source;
        }

        public string ContentType
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public List<System.Net.Cookie> Cookies
        {
            get { throw new NotImplementedException(); }
        }

        public Dictionary<string, string> Headers
        {
            get { throw new NotImplementedException(); }
        }

        public int PaddingLength
        {
            get
            {
                return Result.PaddingLength;
            }
            set
            {
                Result.PaddingLength = value;
            }
        }

        public IRequest RequestContext
        {
            get
            {
                return Result.RequestContext;
            }
            set
            {
                Result.RequestContext = value;
            }
        }

        public object Response
        {
            get
            {
                return Result.Response;
            }
            set
            {
                Result.Response = value;
            }
        }

        public IContentTypeWriter ResponseFilter
        {
            get
            {
                return Result.ResponseFilter;
            }
            set
            {
                Result.ResponseFilter = value;
            }
        }

        public Func<IDisposable> ResultScope
        {
            get
            {
                return Result.ResultScope;
            }
            set
            {
                Result.ResultScope = value;
            }
        }

        public int Status
        {
            get
            {
                return Result.Status;
            }
            set
            {
                Result.Status = value;
            }
        }

        public System.Net.HttpStatusCode StatusCode
        {
            get
            {
                return Result.StatusCode;
            }
            set
            {
                Result.StatusCode = value;
            }
        }

        public string StatusDescription
        {
            get
            {
                return Result.StatusDescription;
            }
            set
            {
                Result.StatusDescription = value;
            }
        }

        private IHttpResult Result
        {
            get
            {
                return _source as IHttpResult;
            }
        }
    }
}
