using System;
using Emby.Drawing;
using Emby.Drawing.Skia;
using Emby.Server.Implementations;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Globalization;

namespace MediaBrowser.Server.Startup.Common
{
    public class ImageEncoderHelper
    {
        public static IImageEncoder GetImageEncoder(ILogger logger,
            ILogManager logManager,
            IFileSystem fileSystem,
            StartupOptions startupOptions,
            Func<IHttpClient> httpClient,
            IApplicationPaths appPaths,
            ILocalizationManager localizationManager)
        {
            try
            {
                return new SkiaEncoder(logManager.GetLogger("Skia"), appPaths, httpClient, fileSystem, localizationManager);
            }
            catch
            {
                logger.Error("Skia not available. Will try next image processor.");
            }

            return new NullImageEncoder();
        }
    }
}
