using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Plugins;
using System;
using System.IO;
using System.Threading;

namespace MediaBrowser.ServerApplication.EntryPoints
{
    public class WanAddressEntryPoint : IServerEntryPoint
    {
        public static string WanAddress;
        private Timer _timer;
        private readonly IHttpClient _httpClient;

        public WanAddressEntryPoint(IHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public void Run()
        {
            _timer = new Timer(TimerCallback, null, TimeSpan.FromMinutes(1), TimeSpan.FromHours(24));
        }

        private async void TimerCallback(object state)
        {
            try
            {
                using (var stream = await _httpClient.Get(new HttpRequestOptions
                {
                    Url = "http://bot.whatismyipaddress.com/"

                }).ConfigureAwait(false))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        WanAddress = await reader.ReadToEndAsync().ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                var b = true;
            }
        }

        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }
    }
}
