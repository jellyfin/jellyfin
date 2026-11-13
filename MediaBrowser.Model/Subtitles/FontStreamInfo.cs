using System;

namespace MediaBrowser.Model.Subtitles
{
    /// <summary>
    /// Class FontStreamInfo.
    /// </summary>
    public class FontStreamInfo
    {
        /// <summary>
        /// Gets or sets the filesystem path of the font file.
        /// </summary>
        /// <value>The filesystem path.</value>
        public required string Path { get; set; }

        /// <summary>
        /// Gets or sets the lookup key that can be used to grab the underlying font file from Jellyfin API.
        /// </summary>
        /// <value>The lookup key.</value>
        public required string Key { get; set; }

        /// <summary>
        /// Gets or sets the font name as specified in the subtitle file that referenced it.
        /// This might be either family name, full name or PostScript name.
        /// It does not uniquely identify the font on its own.
        /// Any given font file may match multiple names.
        /// </summary>
        /// <value>The name.</value>
        public string? FontName { get; set; }

        /// <summary>
        /// Gets or sets the PostScript name of the font.
        /// The PostScript font name is supposed to be unique, locale-independent
        /// and abide by certain restrictions to allowed format.
        /// Unfortunately, not all font files out there physically abide by these standards.
        /// </summary>
        /// <value>The PostScript name.</value>
        public string? PostScriptName { get; set; }

        /// <summary>
        /// Gets or sets the Bold score.
        /// </summary>
        /// <value>The Bold.</value>
        public int Bold { get; set; }

        /// <summary>
        /// Gets or sets the Italic score.
        /// </summary>
        /// <value>The Italic.</value>
        public int Italic { get; set; }
    }
}
