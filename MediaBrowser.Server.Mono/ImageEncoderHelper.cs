using System;
using Emby.Drawing;
using Emby.Drawing.ImageMagick;
using Emby.Server.Implementations;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using Emby.Drawing.Skia;
using MediaBrowser.Model.System;
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
            IEnvironmentInfo environment,
            ILocalizationManager localizationManager)
        {
            if (!startupOptions.ContainsOption("-enablegdi"))
            {
                try
                {
                    return new SkiaEncoder(logManager.GetLogger("Skia"), appPaths, httpClient, fileSystem, localizationManager);
                }
                catch (Exception ex)
                {
                    logger.Info("Skia not available. Will try next image processor. {0}", ex.Message);
                }

                try
                {
                    return new ImageMagickEncoder(logManager.GetLogger("ImageMagick"), appPaths, httpClient, fileSystem, environment);
                }
                catch
                {
                    logger.Info("ImageMagick not available. Will try next image processor.");
                }
            }

            return new NullImageEncoder();
        }
    }
}
