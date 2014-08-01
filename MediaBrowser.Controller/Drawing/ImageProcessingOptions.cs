using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Drawing;
using System.Collections.Generic;
using System.IO;

namespace MediaBrowser.Controller.Drawing
{
    public class ImageProcessingOptions
    {
        public IHasImages Item { get; set; }

        public ItemImageInfo Image { get; set; }

        public int ImageIndex { get; set; }

        public bool CropWhiteSpace { get; set; }

        public int? Width { get; set; }

        public int? Height { get; set; }

        public int? MaxWidth { get; set; }

        public int? MaxHeight { get; set; }

        public int? Quality { get; set; }

        public List<IImageEnhancer> Enhancers { get; set; }

        public ImageOutputFormat OutputFormat { get; set; }

        public bool AddPlayedIndicator { get; set; }

        public int? UnplayedCount { get; set; }

        public double? PercentPlayed { get; set; }

        public string BackgroundColor { get; set; }

        public bool HasDefaultOptions(string originalImagePath)
        {
            return HasDefaultOptionsWithoutSize(originalImagePath) &&
                !Width.HasValue &&
                !Height.HasValue &&
                !MaxWidth.HasValue &&
                !MaxHeight.HasValue;
        }

        public bool HasDefaultOptionsWithoutSize(string originalImagePath)
        {
            return (!Quality.HasValue || Quality.Value == 100) &&
                IsOutputFormatDefault(originalImagePath) &&
                !AddPlayedIndicator &&
                !PercentPlayed.HasValue &&
                !UnplayedCount.HasValue &&
                string.IsNullOrEmpty(BackgroundColor);
        }

        private bool IsOutputFormatDefault(string originalImagePath)
        {
            if (OutputFormat == ImageOutputFormat.Original)
            {
                return true;
            }

            return string.Equals(Path.GetExtension(originalImagePath), "." + OutputFormat);
        }
    }
}
