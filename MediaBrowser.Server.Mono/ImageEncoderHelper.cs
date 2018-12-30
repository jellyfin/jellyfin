using System;
using Emby.Drawing;
using Emby.Drawing.ImageMagick;
using Emby.Server.Implementations;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using Emby.Drawing.Skia;
using MediaBrowser.Model.System;
using MediaBrowser.Model.Globalization;

namespace MediaBrowser.Server.Startup.Common
{
    public class ImageEncoderHelper
    {
        public static IImageEncoder GetImageEncoder(ILogger logger, 
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
                    return new SkiaEncoder(logger, appPaths, httpClient, fileSystem, localizationManager);
                }
                catch (Exception ex)
                {
                    logger.LogInformation("Skia not available. Will try next image processor. {0}", ex.Message);
                }

                try
                {
                    return new ImageMagickEncoder(logger, appPaths, httpClient, fileSystem, environment);
                }
                catch
                {
                    logger.LogInformation("ImageMagick not available. Will try next image processor.");
                }
            }

            return new NullImageEncoder();
        }
    }
}
