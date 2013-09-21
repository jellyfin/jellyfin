using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Drawing
{
    public class ImageProcessingOptions
    {
        public BaseItem Item { get; set; }

        public ImageType ImageType { get; set; }

        public int ImageIndex { get; set; }

        public string OriginalImagePath { get; set; }

        public DateTime OriginalImageDateModified { get; set; }

        public bool CropWhiteSpace { get; set; }

        public int? Width { get; set; }

        public int? Height { get; set; }

        public int? MaxWidth { get; set; }

        public int? MaxHeight { get; set; }

        public int? Quality { get; set; }

        public List<IImageEnhancer> Enhancers { get; set; }

        public ImageOutputFormat OutputFormat { get; set; }

        public ImageOverlay? Indicator { get; set; }

        public int PercentPlayed { get; set; }

        public string BackgroundColor { get; set; }
    }
}
