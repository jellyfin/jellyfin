using System.Collections.Generic;

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
        /// <param name="portraitInputPaths">The portrait input paths.</param>
        /// <param name="landscapeInputPaths">The landscape input paths.</param>
        /// <param name="outputPath">The output path.</param>
        /// <param name="width">Optional. The image width.</param>
        /// <param name="height">Optional. The image height.</param>
        /// <param name="applyFilter">Optional. Apply a darkening filter.</param>
        public SplashscreenOptions(IReadOnlyList<string> portraitInputPaths, IReadOnlyList<string> landscapeInputPaths, string outputPath, int width = 1920, int height = 1080, bool applyFilter = false)
        {
            PortraitInputPaths = portraitInputPaths;
            LandscapeInputPaths = landscapeInputPaths;
            OutputPath = outputPath;
            Width = width;
            Height = height;
            ApplyFilter = applyFilter;
        }

        /// <summary>
        /// Gets or sets the poster input paths.
        /// </summary>
        public IReadOnlyList<string> PortraitInputPaths { get; set; }

        /// <summary>
        /// Gets or sets the landscape input paths.
        /// </summary>
        public IReadOnlyList<string> LandscapeInputPaths { get; set; }

        /// <summary>
        /// Gets or sets the output path.
        /// </summary>
        public string OutputPath { get; set; }

        /// <summary>
        /// Gets or sets the width.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Gets or sets the height.
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to apply a darkening filter at the end.
        /// </summary>
        public bool ApplyFilter { get; set; }
    }
}
