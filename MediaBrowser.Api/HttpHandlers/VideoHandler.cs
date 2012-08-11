using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using MediaBrowser.Common.Logging;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Api.HttpHandlers
{
    class VideoHandler : BaseMediaHandler<Video>
    {
        public IEnumerable<string> VideoFormats
        {
            get
            {
                return QueryString["videoformats"].Split(',');
            }
        }

        /// <summary>
        /// Gets the format we'll be converting to
        /// </summary>
        protected override string GetOutputFormat()
        {
            return VideoFormats.First();
        }

        protected override bool RequiresConversion()
        {
            // If it's not in a format the consumer accepts, return true
            if (!VideoFormats.Any(f => LibraryItem.Path.EndsWith(f, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
            
            AudioStream audio = LibraryItem.AudioStreams.FirstOrDefault();

            if (audio != null)
            {
                // If the number of channels is greater than our desired channels, we need to transcode
                if (AudioChannels.HasValue && AudioChannels.Value < audio.Channels)
                {
                    return true;
                }
            }

            // Yay
            return false;
        }

        protected override Task WriteResponseToOutputStream(Stream stream)
        {
            throw new NotImplementedException();
        }
    }
}
