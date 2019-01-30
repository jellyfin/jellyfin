using System;
using MediaBrowser.Controller.Library;
using TvDbSharper;

namespace MediaBrowser.Providers.TV
{
    public sealed class TvDbClientManager
    {
        private static volatile TvDbClientManager instance;
        private static readonly object syncRoot = new object();
        private static TvDbClient tvDbClient;
        private static DateTime tokenCreatedAt;

        private TvDbClientManager()
        {
            tvDbClient = new TvDbClient();
            tvDbClient.Authentication.AuthenticateAsync(TVUtils.TvdbApiKey);
            tokenCreatedAt = DateTime.Now;
        }

        public static TvDbClientManager Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                lock (syncRoot)
                {
                    if (instance == null)
                        instance = new TvDbClientManager();
                }

                return instance;
            }
        }

        public TvDbClient TvDbClient
        {
            get
            {
                // Refresh if necessary
                if (tokenCreatedAt > DateTime.Now.Subtract(TimeSpan.FromHours(20)))
                {
                    try
                    {
                        tvDbClient.Authentication.RefreshTokenAsync();
                    }
                    catch
                    {
                        tvDbClient.Authentication.AuthenticateAsync(TVUtils.TvdbApiKey);
                    }

                    tokenCreatedAt = DateTime.Now;
                }
                // Default to English
                tvDbClient.AcceptedLanguage = "en";
                return tvDbClient;
            }
        }
    }
}
