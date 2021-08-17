namespace MediaBrowser.Controller.Drawing
{
    /// <summary>
    /// Options used to generate the splashscreen.
    /// </summary>
    public class SplashscreenOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SplashscreenOptions"/> class.
        /// </summary>
        /// <param name="outputPath">The output path.</param>
        /// <param name="applyFilter">Optional. Apply a darkening filter.</param>
        public SplashscreenOptions(string outputPath, bool applyFilter = false)
        {
            OutputPath = outputPath;
            ApplyFilter = applyFilter;
        }

        /// <summary>
        /// Gets or sets the output path.
        /// </summary>
        public string OutputPath { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to apply a darkening filter at the end.
        /// </summary>
        public bool ApplyFilter { get; set; }
    }
}
