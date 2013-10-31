using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.LiveTv;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Plugins.NextPvr
{
    /// <summary>
    /// Class LiveTvService
    /// </summary>
    public class LiveTvService : ILiveTvService
    {
        private readonly ILogger _logger;

        private IApplicationPaths _appPaths;
        private IJsonSerializer _json;
        private IHttpClient _httpClient;

        public LiveTvService(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gets the channels async.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{ChannelInfo}}.</returns>
        public Task<IEnumerable<ChannelInfo>> GetChannelsAsync(CancellationToken cancellationToken)
        {
            //using (var stream = await _httpClient.Get(new HttpRequestOptions()
            //    {
            //          Url = "",
            //          CancellationToken = cancellationToken
            //    }))
            //{
                
            //}
            _logger.Info("GetChannelsAsync");

            var channels = new List<ChannelInfo>
                {
                    new ChannelInfo
                        {
                             Name = "NBC",
                              ServiceName = Name
                        }
                };

            return Task.FromResult<IEnumerable<ChannelInfo>>(channels);
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return "Next Pvr"; }
        }
    }
}
