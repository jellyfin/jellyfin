using MediaBrowser.Controller;
using MediaBrowser.Model.Logging;
using ServiceStack.Text.Controller;
using ServiceStack.Web;
using System;
using System.IO;
using System.Linq;

namespace MediaBrowser.Api.Playback.Hls
{
    public class HlsSegmentResponseFilter : Attribute, IHasResponseFilter
    {
        public ILogger Logger { get; set; }
        public IServerApplicationPaths ApplicationPaths { get; set; }

        public void ResponseFilter(IRequest req, IResponse res, object response)
        {
            var pathInfo = PathInfo.Parse(req.PathInfo);
            var itemId = pathInfo.GetArgumentValue<string>(1);
            var playlistId = pathInfo.GetArgumentValue<string>(3);

            OnEndRequest(itemId, playlistId);
        }

        public IHasResponseFilter Copy()
        {
            return this;
        }

        public int Priority
        {
            get { return -1; }
        }

        /// <summary>
        /// Called when [end request].
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="playlistId">The playlist id.</param>
        protected void OnEndRequest(string itemId, string playlistId)
        {
            Logger.Info("OnEndRequest " + playlistId);
            var normalizedPlaylistId = playlistId.Replace("-low", string.Empty);

            foreach (var playlist in Directory.EnumerateFiles(ApplicationPaths.EncodedMediaCachePath, "*.m3u8")
                .Where(i => i.IndexOf(normalizedPlaylistId, StringComparison.OrdinalIgnoreCase) != -1)
                .ToList())
            {
                ApiEntryPoint.Instance.OnTranscodeEndRequest(playlist, TranscodingJobType.Hls);
            }
        }
    }
}
