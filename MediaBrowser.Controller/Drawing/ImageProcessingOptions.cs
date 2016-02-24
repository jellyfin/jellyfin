using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Drawing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

        public int Quality { get; set; }

        public List<IImageEnhancer> Enhancers { get; set; }

        public List<ImageFormat> SupportedOutputFormats { get; set; }

        public bool AddPlayedIndicator { get; set; }

        public int? UnplayedCount { get; set; }

        public double PercentPlayed { get; set; }

        public string BackgroundColor { get; set; }
        public string ForegroundLayer { get; set; }

        public bool HasDefaultOptions(string originalImagePath)
        {
            return HasDefaultOptionsWithoutSize(originalImagePath) &&
                !Width.HasValue &&
                !Height.HasValue &&
                !MaxWidth.HasValue &&
                !MaxHeight.HasValue;
        }

        public bool HasDefaultOptions(string originalImagePath, ImageSize size)
        {
            if (!HasDefaultOptionsWithoutSize(originalImagePath))
            {
                return false;
            }

            if (Width.HasValue && !size.Width.Equals(Width.Value))
            {
                return false;
            }
            if (Height.HasValue && !size.Height.Equals(Height.Value))
            {
                return false;
            }
            if (MaxWidth.HasValue && size.Width > MaxWidth.Value)
            {
                return false;
            }
            if (MaxHeight.HasValue && size.Height > MaxHeight.Value)
            {
                return false;
            }

            return true;
        }

        public bool HasDefaultOptionsWithoutSize(string originalImagePath)
        {
            return (Quality >= 90) &&
                IsFormatSupported(originalImagePath) &&
                !AddPlayedIndicator &&
                PercentPlayed.Equals(0) &&
                !UnplayedCount.HasValue &&
                string.IsNullOrEmpty(BackgroundColor) &&
                string.IsNullOrEmpty(ForegroundLayer);
        }

        private bool IsFormatSupported(string originalImagePath)
        {
            var ext = Path.GetExtension(originalImagePath);
            return SupportedOutputFormats.Any(outputFormat => string.Equals(ext, "." + outputFormat, StringComparison.OrdinalIgnoreCase));
        }
    }
}
