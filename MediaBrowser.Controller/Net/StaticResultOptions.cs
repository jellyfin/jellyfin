using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Net
{
    public class StaticResultOptions
    {
        public string ContentType { get; set; }
        public TimeSpan? CacheDuration { get; set; }
        public DateTime? DateLastModified { get; set; }
        public Guid CacheKey { get; set; }

        public Func<Task<Stream>> ContentFactory { get; set; }

        public bool IsHeadRequest { get; set; }

        public IDictionary<string, string> ResponseHeaders { get; set; }

        public Action OnComplete { get; set; }
        public Action OnError { get; set; }

        public StaticResultOptions()
        {
            ResponseHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public class StaticFileResultOptions : StaticResultOptions
    {
        public string Path { get; set; }

        public FileShare FileShare { get; set; }

        public StaticFileResultOptions()
        {
            FileShare = FileShare.Read;
        }
    }
}
