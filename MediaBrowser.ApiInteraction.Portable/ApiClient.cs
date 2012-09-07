using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

namespace MediaBrowser.ApiInteraction.Portable
{
    public class ApiClient : BaseApiClient
    {
        private HttpWebRequest GetNewRequest(string url)
        {
            HttpWebRequest request = HttpWebRequest.CreateHttp(url);
        }
    }
}
