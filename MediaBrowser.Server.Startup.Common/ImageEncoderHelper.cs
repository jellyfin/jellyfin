using System;
using Emby.Drawing;
using Emby.Drawing.Net;
using Emby.Drawing.ImageMagick;
using Emby.Server.Core;
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
            if (!startupOptions.ContainsOption("-enablegdi"))
            {
                try
                {
                    return new ImageMagickEncoder(logManager.GetLogger("ImageMagick"), appPaths, httpClient, fileSystem);
                }
                catch
                {
                    logger.Error("Error loading ImageMagick. Will revert to GDI.");
                }
            }

            try
            {
                return new GDIImageEncoder(fileSystem, logManager.GetLogger("GDI"));
            }
            catch
            {
                logger.Error("Error loading GDI. Will revert to NullImageEncoder.");
            }

            return new NullImageEncoder();
        }
    }
}
