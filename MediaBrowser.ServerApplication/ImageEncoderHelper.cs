using System;
using Emby.Drawing;
using Emby.Drawing.ImageMagick;
using Emby.Drawing.Skia;
using Emby.Server.Core;
using Emby.Server.Implementations;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.Server.Startup.Common
{
    public class ImageEncoderHelper
    {
        public static IImageEncoder GetImageEncoder(ILogger logger, 
            ILogManager logManager, 
            IFileSystem fileSystem, 
            StartupOptions startupOptions, 
            Func<IHttpClient> httpClient,
            IApplicationPaths appPaths)
        {
            try
            {
                return new SkiaEncoder(logManager.GetLogger("Skia"), appPaths, httpClient, fileSystem);
            }
            catch
            {
                logger.Error("Error loading Skia. Will revert to ImageMagick.");
            }

            try
            {
                return new ImageMagickEncoder(logManager.GetLogger("ImageMagick"), appPaths, httpClient, fileSystem);
            }
            catch
            {
                logger.Error("Error loading ImageMagick. Will revert to GDI.");
            }

            return new NullImageEncoder();
        }
    }
}
