using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Jellyfin.Api.Helpers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Lyrics;

namespace MediaBrowser.Providers.Lyric
{
    /// <summary>
    /// TXT Lyric Provider.
    /// </summary>
    public class TxtLyricProvider : ILyricProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TxtLyricProvider"/> class.
        /// </summary>
        public TxtLyricProvider()
        {
            Name = "TxtLyricProvider";

            SupportedMediaTypes = new Collection<string>
            {
                "lrc", "txt"
            };
        }

        /// <summary>
        /// Gets a value indicating the provider name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets a value indicating the File Extenstions this provider supports.
        /// </summary>
        public IEnumerable<string> SupportedMediaTypes { get; }

        /// <summary>
        /// Opens lyric file for the requested item, and processes it for API return.
        /// </summary>
        /// <param name="item">The item to to process.</param>
        /// <returns>If provider can determine lyrics, returns a <see cref="LyricResponse"/>; otherwise, null.</returns>
        public LyricResponse? GetLyrics(BaseItem item)
        {
            string? lyricFilePath = LyricInfo.GetLyricFilePath(this, item.Path);

            if (string.IsNullOrEmpty(lyricFilePath))
            {
                return null;
            }

            List<Controller.Lyrics.Lyric> lyricList = new List<Controller.Lyrics.Lyric>();

            string lyricData = System.IO.File.ReadAllText(lyricFilePath);

            // Splitting on Environment.NewLine caused some new lines to be missed in Windows.
            char[] newLineDelims = new[] { '\r', '\n' };
            string[] lyricTextLines = lyricData.Split(newLineDelims, StringSplitOptions.RemoveEmptyEntries);

            if (!lyricTextLines.Any())
            {
                return null;
            }

            foreach (string lyricTextLine in lyricTextLines)
            {
                lyricList.Add(new Controller.Lyrics.Lyric { Text = lyricTextLine });
            }

            return new LyricResponse { Lyrics = lyricList };
        }
    }
}
