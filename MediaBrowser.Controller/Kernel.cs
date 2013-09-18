using MediaBrowser.Controller.MediaInfo;

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
        /// Gets the FFMPEG controller.
        /// </summary>
        /// <value>The FFMPEG controller.</value>
        public FFMpegManager FFMpegManager { get; set; }

        /// <summary>
        /// Creates a kernel based on a Data path, which is akin to our current programdata path
        /// </summary>
        public Kernel()
        {
            Instance = this;
        }
    }
}
