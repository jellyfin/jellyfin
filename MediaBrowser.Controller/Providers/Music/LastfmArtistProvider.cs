using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Controller.Providers.Music
{
    public class LastfmArtistProvider : LastfmBaseArtistProvider
    {
        public LastfmArtistProvider(IJsonSerializer jsonSerializer, IHttpClient httpClient, ILogManager logManager) 
            : base(jsonSerializer, httpClient, logManager)
        {
        }

        protected override Task FetchLastfmData(BaseItem item, string id, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
