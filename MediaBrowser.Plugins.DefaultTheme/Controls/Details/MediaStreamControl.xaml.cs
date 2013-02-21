using MediaBrowser.Model.Entities;
using MediaBrowser.UI.Controls;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows.Controls;

namespace MediaBrowser.Plugins.DefaultTheme.Controls.Details
{
    /// <summary>
    /// Interaction logic for MediaStreamControl.xaml
    /// </summary>
    public partial class MediaStreamControl : BaseUserControl
    {
        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaStreamControl" /> class.
        /// </summary>
        public MediaStreamControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// The _media stream
        /// </summary>
        private MediaStream _mediaStream;
        /// <summary>
        /// Gets or sets the media stream.
        /// </summary>
        /// <value>The media stream.</value>
        public MediaStream MediaStream
        {
            get { return _mediaStream; }
            set
            {
                _mediaStream = value;
                OnPropertyChanged("MediaStream");
                OnStreamChanged();
            }
        }

        /// <summary>
        /// Called when [stream changed].
        /// </summary>
        private void OnStreamChanged()
        {
            if (MediaStream == null)
            {
                StreamName.Text = string.Empty;
                StreamDetails.Children.Clear();
                return;
            }

            var details = new List<string> { };

            if (MediaStream.Type != MediaStreamType.Video)
            {
                AddDetail(details, MediaStream.Language);
            }

            if (!string.IsNullOrEmpty(MediaStream.Path))
            {
                AddDetail(details, Path.GetFileName(MediaStream.Path));
            }

            if (MediaStream.Type == MediaStreamType.Video)
            {
                var resolution = string.Format("{0}*{1}", MediaStream.Width, MediaStream.Height);

                AddDetail(details, resolution);
            }

            AddDetail(details, MediaStream.AspectRatio);

            if (MediaStream.Type != MediaStreamType.Video)
            {
                if (MediaStream.IsDefault)
                {
                    //AddDetail(details, "default");
                }
                if (MediaStream.IsForced)
                {
                    AddDetail(details, "forced");
                }
            }

            AddDetail(details, MediaStream.Codec);

            if (MediaStream.Channels.HasValue)
            {
                AddDetail(details, MediaStream.Channels.Value.ToString(UsCulture) + "ch");
            }
            
            if (MediaStream.BitRate.HasValue)
            {
                var text = (MediaStream.BitRate.Value / 1000).ToString(UsCulture) + "kbps";

                AddDetail(details, text);
            }

            var framerate = MediaStream.AverageFrameRate ?? MediaStream.RealFrameRate ?? 0;

            if (framerate > 0)
            {
                AddDetail(details, framerate.ToString(UsCulture));
            }

            if (MediaStream.SampleRate.HasValue)
            {
                AddDetail(details, MediaStream.SampleRate.Value.ToString(UsCulture) + "khz");
            }

            AddDetail(string.Join(" \u2022 ", details.ToArray()));

            StreamName.Text = MediaStream.Type.ToString();
        }

        private void AddDetail(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            var control = new TextBlock() { Text = text };
            control.SetResourceReference(StyleProperty, "TextBlockStyle");
            StreamDetails.Children.Add(control);
        }

        private void AddDetail(ICollection<string> list, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            list.Add(text);
        }
    }
}
