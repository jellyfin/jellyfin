using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.MediaInfo;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Weather;
using System.Collections.Generic;

namespace MediaBrowser.Controller
{
    /// <summary>
    /// Class Kernel
    /// </summary>
    public class Kernel 
    {
        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static Kernel Instance { get; private set; }

        /// <summary>
        /// Gets the image manager.
        /// </summary>
        /// <value>The image manager.</value>
        public ImageManager ImageManager { get; set; }

        /// <summary>
        /// Gets the FFMPEG controller.
        /// </summary>
        /// <value>The FFMPEG controller.</value>
        public FFMpegManager FFMpegManager { get; set; }

        /// <summary>
        /// Gets the name of the web application that can be used for url building.
        /// All api urls will be of the form {protocol}://{host}:{port}/{appname}/...
        /// </summary>
        /// <value>The name of the web application.</value>
        public string WebApplicationName
        {
            get { return "mediabrowser"; }
        }

        /// <summary>
        /// Gets the HTTP server URL prefix.
        /// </summary>
        /// <value>The HTTP server URL prefix.</value>
        public virtual string HttpServerUrlPrefix
        {
            get
            {
                return "http://+:" + _configurationManager.Configuration.HttpServerPortNumber + "/" + WebApplicationName + "/";
            }
        }

        /// <summary>
        /// Gets the list of Localized string files
        /// </summary>
        /// <value>The string files.</value>
        public IEnumerable<LocalizedStringData> StringFiles { get; set; }

        /// <summary>
        /// Gets the list of currently registered weather prvoiders
        /// </summary>
        /// <value>The weather providers.</value>
        public IEnumerable<IWeatherProvider> WeatherProviders { get; set; }

        /// <summary>
        /// Gets the list of currently registered image processors
        /// Image processors are specialized metadata providers that run after the normal ones
        /// </summary>
        /// <value>The image enhancers.</value>
        public IEnumerable<IImageEnhancer> ImageEnhancers { get; set; }

        private readonly IServerConfigurationManager _configurationManager;

        /// <summary>
        /// Creates a kernel based on a Data path, which is akin to our current programdata path
        /// </summary>
        /// <param name="configurationManager">The configuration manager.</param>
        public Kernel(IServerConfigurationManager configurationManager)
        {
            Instance = this;

            _configurationManager = configurationManager;
        }
    }
}
