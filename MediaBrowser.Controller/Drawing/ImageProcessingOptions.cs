using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Drawing;

namespace MediaBrowser.Controller.Drawing
{
    public class ImageProcessingOptions
    {
        public ImageProcessingOptions()
        {
            RequiresAutoOrientation = true;
        }

        public Guid ItemId { get; set; }
        public BaseItem Item { get; set; }

        public ItemImageInfo Image { get; set; }

        public int ImageIndex { get; set; }

        public bool CropWhiteSpace { get; set; }

        public int? Width { get; set; }

        public int? Height { get; set; }

        public int? MaxWidth { get; set; }

        public int? MaxHeight { get; set; }

        public int Quality { get; set; }

        public IReadOnlyCollection<ImageFormat> SupportedOutputFormats { get; set; }

        public bool AddPlayedIndicator { get; set; }

        public int? UnplayedCount { get; set; }
        public int? Blur { get; set; }

        public double PercentPlayed { get; set; }

        public string BackgroundColor { get; set; }
        public string ForegroundLayer { get; set; }
        public bool RequiresAutoOrientation { get; set; }

        private bool HasDefaultOptions(string originalImagePath)
        {
            return HasDefaultOptionsWithoutSize(originalImagePath) &&
                !Width.HasValue &&
                !Height.HasValue &&
                !MaxWidth.HasValue &&
                !MaxHeight.HasValue;
        }

        public bool HasDefaultOptions(string originalImagePath, ImageDimensions? size)
        {
            if (!size.HasValue)
            {
                return HasDefaultOptions(originalImagePath);
            }

            if (!HasDefaultOptionsWithoutSize(originalImagePath))
            {
                return false;
            }

            var sizeValue = size.Value;

            if (Width.HasValue && !sizeValue.Width.Equals(Width.Value))
            {
                return false;
            }
            if (Height.HasValue && !sizeValue.Height.Equals(Height.Value))
            {
                return false;
            }
            if (MaxWidth.HasValue && sizeValue.Width > MaxWidth.Value)
            {
                return false;
            }
            if (MaxHeight.HasValue && sizeValue.Height > MaxHeight.Value)
            {
                return false;
            }

            return true;
        }

        private bool HasDefaultOptionsWithoutSize(string originalImagePath)
        {
            return (Quality >= 90) &&
                IsFormatSupported(originalImagePath) &&
                !AddPlayedIndicator &&
                PercentPlayed.Equals(0) &&
                !UnplayedCount.HasValue &&
                !Blur.HasValue &&
                !CropWhiteSpace &&
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
