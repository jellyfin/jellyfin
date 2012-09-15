using MediaBrowser.Model.DTO;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MediaBrowser.ApiInteraction
{
    /// <summary>
    /// Provides api methods that are usable on all platforms
    /// </summary>
    public abstract class BaseApiClient : IDisposable
    {
        protected BaseApiClient()
        {
            DataSerializer.Configure();
        }

        /// <summary>
        /// Gets or sets the server host name (myserver or 192.168.x.x)
        /// </summary>
        public string ServerHostName { get; set; }

        /// <summary>
        /// Gets or sets the port number used by the API
        /// </summary>
        public int ServerApiPort { get; set; }

        /// <summary>
        /// Gets the current api url based on hostname and port.
        /// </summary>
        protected string ApiUrl
        {
            get
            {
                return string.Format("http://{0}:{1}/mediabrowser/api", ServerHostName, ServerApiPort);
            }
        }

        /// <summary>
        /// Gets the default data format to request from the server
        /// </summary>
        protected SerializationFormats SerializationFormat
        {
            get
            {
                return SerializationFormats.Protobuf;
            }
        }

        /// <summary>
        /// Gets an image url that can be used to download an image from the api
        /// </summary>
        /// <param name="itemId">The Id of the item</param>
        /// <param name="imageType">The type of image requested</param>
        /// <param name="imageIndex">The image index, if there are multiple. Currently only applies to backdrops. Supply null or 0 for first backdrop.</param>
        /// <param name="width">Use if a fixed width is required. Aspect ratio will be preserved.</param>
        /// <param name="height">Use if a fixed height is required. Aspect ratio will be preserved.</param>
        /// <param name="maxWidth">Use if a max width is required. Aspect ratio will be preserved.</param>
        /// <param name="maxHeight">Use if a max height is required. Aspect ratio will be preserved.</param>
        /// <param name="quality">Quality level, from 0-100. Currently only applies to JPG. The default value should suffice.</param>
        public string GetImageUrl(Guid itemId, ImageType imageType, int? imageIndex = null, int? width = null, int? height = null, int? maxWidth = null, int? maxHeight = null, int? quality = null)
        {
            string url = ApiUrl + "/image";

            url += "?id=" + itemId.ToString();
            url += "&type=" + imageType.ToString();

            if (imageIndex.HasValue)
            {
                url += "&index=" + imageIndex;
            }
            if (width.HasValue)
            {
                url += "&width=" + width;
            }
            if (height.HasValue)
            {
                url += "&height=" + height;
            }
            if (maxWidth.HasValue)
            {
                url += "&maxWidth=" + maxWidth;
            }
            if (maxHeight.HasValue)
            {
                url += "&maxHeight=" + maxHeight;
            }
            if (quality.HasValue)
            {
                url += "&quality=" + quality;
            }

            return url;
        }

        /// <summary>
        /// Gets an image url that can be used to download an image from the api
        /// </summary>
        /// <param name="userId">The Id of the user</param>
        /// <param name="width">Use if a fixed width is required. Aspect ratio will be preserved.</param>
        /// <param name="height">Use if a fixed height is required. Aspect ratio will be preserved.</param>
        /// <param name="maxWidth">Use if a max width is required. Aspect ratio will be preserved.</param>
        /// <param name="maxHeight">Use if a max height is required. Aspect ratio will be preserved.</param>
        /// <param name="quality">Quality level, from 0-100. Currently only applies to JPG. The default value should suffice.</param>
        public string GetUserImageUrl(Guid userId, int? width = null, int? height = null, int? maxWidth = null, int? maxHeight = null, int? quality = null)
        {
            string url = ApiUrl + "/image";

            url += "?userId=" + userId.ToString();

            if (width.HasValue)
            {
                url += "&width=" + width;
            }
            if (height.HasValue)
            {
                url += "&height=" + height;
            }
            if (maxWidth.HasValue)
            {
                url += "&maxWidth=" + maxWidth;
            }
            if (maxHeight.HasValue)
            {
                url += "&maxHeight=" + maxHeight;
            }
            if (quality.HasValue)
            {
                url += "&quality=" + quality;
            }

            return url;
        }

        /// <summary>
        /// Gets an image url that can be used to download an image from the api
        /// </summary>
        /// <param name="name">The name of the person</param>
        /// <param name="width">Use if a fixed width is required. Aspect ratio will be preserved.</param>
        /// <param name="height">Use if a fixed height is required. Aspect ratio will be preserved.</param>
        /// <param name="maxWidth">Use if a max width is required. Aspect ratio will be preserved.</param>
        /// <param name="maxHeight">Use if a max height is required. Aspect ratio will be preserved.</param>
        /// <param name="quality">Quality level, from 0-100. Currently only applies to JPG. The default value should suffice.</param>
        public string GetPersonImageUrl(string name, int? width = null, int? height = null, int? maxWidth = null, int? maxHeight = null, int? quality = null)
        {
            string url = ApiUrl + "/image";

            url += "?personname=" + name;

            if (width.HasValue)
            {
                url += "&width=" + width;
            }
            if (height.HasValue)
            {
                url += "&height=" + height;
            }
            if (maxWidth.HasValue)
            {
                url += "&maxWidth=" + maxWidth;
            }
            if (maxHeight.HasValue)
            {
                url += "&maxHeight=" + maxHeight;
            }
            if (quality.HasValue)
            {
                url += "&quality=" + quality;
            }

            return url;
        }

        /// <summary>
        /// Gets an image url that can be used to download an image from the api
        /// </summary>
        /// <param name="year">The year</param>
        /// <param name="width">Use if a fixed width is required. Aspect ratio will be preserved.</param>
        /// <param name="height">Use if a fixed height is required. Aspect ratio will be preserved.</param>
        /// <param name="maxWidth">Use if a max width is required. Aspect ratio will be preserved.</param>
        /// <param name="maxHeight">Use if a max height is required. Aspect ratio will be preserved.</param>
        /// <param name="quality">Quality level, from 0-100. Currently only applies to JPG. The default value should suffice.</param>
        public string GetYearImageUrl(int year, int? width = null, int? height = null, int? maxWidth = null, int? maxHeight = null, int? quality = null)
        {
            string url = ApiUrl + "/image";

            url += "?year=" + year;

            if (width.HasValue)
            {
                url += "&width=" + width;
            }
            if (height.HasValue)
            {
                url += "&height=" + height;
            }
            if (maxWidth.HasValue)
            {
                url += "&maxWidth=" + maxWidth;
            }
            if (maxHeight.HasValue)
            {
                url += "&maxHeight=" + maxHeight;
            }
            if (quality.HasValue)
            {
                url += "&quality=" + quality;
            }

            return url;
        }

        /// <summary>
        /// Gets an image url that can be used to download an image from the api
        /// </summary>
        /// <param name="name">The name of the genre</param>
        /// <param name="width">Use if a fixed width is required. Aspect ratio will be preserved.</param>
        /// <param name="height">Use if a fixed height is required. Aspect ratio will be preserved.</param>
        /// <param name="maxWidth">Use if a max width is required. Aspect ratio will be preserved.</param>
        /// <param name="maxHeight">Use if a max height is required. Aspect ratio will be preserved.</param>
        /// <param name="quality">Quality level, from 0-100. Currently only applies to JPG. The default value should suffice.</param>
        public string GetGenreImageUrl(string name, int? width = null, int? height = null, int? maxWidth = null, int? maxHeight = null, int? quality = null)
        {
            string url = ApiUrl + "/image";

            url += "?genre=" + name;

            if (width.HasValue)
            {
                url += "&width=" + width;
            }
            if (height.HasValue)
            {
                url += "&height=" + height;
            }
            if (maxWidth.HasValue)
            {
                url += "&maxWidth=" + maxWidth;
            }
            if (maxHeight.HasValue)
            {
                url += "&maxHeight=" + maxHeight;
            }
            if (quality.HasValue)
            {
                url += "&quality=" + quality;
            }

            return url;
        }

        /// <summary>
        /// Gets an image url that can be used to download an image from the api
        /// </summary>
        /// <param name="name">The name of the studio</param>
        /// <param name="width">Use if a fixed width is required. Aspect ratio will be preserved.</param>
        /// <param name="height">Use if a fixed height is required. Aspect ratio will be preserved.</param>
        /// <param name="maxWidth">Use if a max width is required. Aspect ratio will be preserved.</param>
        /// <param name="maxHeight">Use if a max height is required. Aspect ratio will be preserved.</param>
        /// <param name="quality">Quality level, from 0-100. Currently only applies to JPG. The default value should suffice.</param>
        public string GetStudioImageUrl(string name, int? width = null, int? height = null, int? maxWidth = null, int? maxHeight = null, int? quality = null)
        {
            string url = ApiUrl + "/image";

            url += "?studio=" + name;

            if (width.HasValue)
            {
                url += "&width=" + width;
            }
            if (height.HasValue)
            {
                url += "&height=" + height;
            }
            if (maxWidth.HasValue)
            {
                url += "&maxWidth=" + maxWidth;
            }
            if (maxHeight.HasValue)
            {
                url += "&maxHeight=" + maxHeight;
            }
            if (quality.HasValue)
            {
                url += "&quality=" + quality;
            }

            return url;
        }

        /// <summary>
        /// This is a helper to get a list of backdrop url's from a given ApiBaseItemWrapper. If the actual item does not have any backdrops it will return backdrops from the first parent that does.
        /// </summary>
        /// <param name="item">A given item.</param>
        /// <param name="width">Use if a fixed width is required. Aspect ratio will be preserved.</param>
        /// <param name="height">Use if a fixed height is required. Aspect ratio will be preserved.</param>
        /// <param name="maxWidth">Use if a max width is required. Aspect ratio will be preserved.</param>
        /// <param name="maxHeight">Use if a max height is required. Aspect ratio will be preserved.</param>
        /// <param name="quality">Quality level, from 0-100. Currently only applies to JPG. The default value should suffice.</param>
        public string[] GetBackdropImageUrls(DtoBaseItem item, int? width = null, int? height = null, int? maxWidth = null, int? maxHeight = null, int? quality = null)
        {
            Guid? backdropItemId;
            int backdropCount;

            if (item.BackdropCount == 0)
            {
                backdropItemId = item.ParentBackdropItemId;
                backdropCount = item.ParentBackdropCount ?? 0;
            }
            else
            {
                backdropItemId = item.Id;
                backdropCount = item.BackdropCount;
            }

            if (backdropItemId == null)
            {
                return new string[] { };
            }

            var files = new string[backdropCount];

            for (int i = 0; i < backdropCount; i++)
            {
                files[i] = GetImageUrl(backdropItemId.Value, ImageType.Backdrop, i, width, height, maxWidth, maxHeight, quality);
            }

            return files;
        }

        /// <summary>
        /// This is a helper to get the logo image url from a given ApiBaseItemWrapper. If the actual item does not have a logo, it will return the logo from the first parent that does, or null.
        /// </summary>
        /// <param name="item">A given item.</param>
        /// <param name="width">Use if a fixed width is required. Aspect ratio will be preserved.</param>
        /// <param name="height">Use if a fixed height is required. Aspect ratio will be preserved.</param>
        /// <param name="maxWidth">Use if a max width is required. Aspect ratio will be preserved.</param>
        /// <param name="maxHeight">Use if a max height is required. Aspect ratio will be preserved.</param>
        /// <param name="quality">Quality level, from 0-100. Currently only applies to JPG. The default value should suffice.</param>
        public string GetLogoImageUrl(DtoBaseItem item, int? width = null, int? height = null, int? maxWidth = null, int? maxHeight = null, int? quality = null)
        {
            Guid? logoItemId = item.HasLogo ? item.Id : item.ParentLogoItemId;

            if (logoItemId.HasValue)
            {
                return GetImageUrl(logoItemId.Value, ImageType.Logo, null, width, height, maxWidth, maxHeight, quality);
            }

            return null;
        }

        /// <summary>
        /// Gets the url needed to stream an audio file
        /// </summary>
        /// <param name="itemId">The id of the item</param>
        /// <param name="supportedOutputFormats">List all the output formats the decice is capable of playing. The more, the better, as it will decrease the likelyhood of having to encode, which will put a load on the server.</param>
        /// <param name="maxAudioChannels">The maximum number of channels that the device can play. Omit this if it doesn't matter. Phones and tablets should generally specify 2.</param>
        /// <param name="maxAudioSampleRate">The maximum sample rate that the device can play. This should generally be omitted. The server will default this to 44100, so only override if a different max is needed.</param>
        public string GetAudioStreamUrl(Guid itemId, IEnumerable<AudioOutputFormats> supportedOutputFormats, int? maxAudioChannels = null, int? maxAudioSampleRate = null)
        {
            string url = ApiUrl + "/audio?id=" + itemId;

            url += "&outputformats=" + string.Join(",", supportedOutputFormats.Select(s => s.ToString()).ToArray());

            if (maxAudioChannels.HasValue)
            {
                url += "&audiochannels=" + maxAudioChannels.Value;
            }

            if (maxAudioSampleRate.HasValue)
            {
                url += "&audiosamplerate=" + maxAudioSampleRate.Value;
            }

            return url;
        }

        /// <summary>
        /// Gets the url needed to stream a video file
        /// </summary>
        /// <param name="itemId">The id of the item</param>
        /// <param name="supportedOutputFormats">List all the output formats the decice is capable of playing. The more, the better, as it will decrease the likelyhood of having to encode, which will put a load on the server.</param>
        /// <param name="maxAudioChannels">The maximum number of channels that the device can play. Omit this if it doesn't matter. Phones and tablets should generally specify 2.</param>
        /// <param name="maxAudioSampleRate">The maximum sample rate that the device can play. This should generally be omitted. The server will default this to 44100, so only override if a different max is needed.</param>
        /// <param name="width">Specify this is a fixed video width is required</param>
        /// <param name="height">Specify this is a fixed video height is required</param>
        /// <param name="maxWidth">Specify this is a max video width is required</param>
        /// <param name="maxHeight">Specify this is a max video height is required</param>
        public string GetVideoStreamUrl(Guid itemId, 
            IEnumerable<VideoOutputFormats> supportedOutputFormats, 
            int? maxAudioChannels = null, 
            int? maxAudioSampleRate = null, 
            int? width = null, 
            int? height = null, 
            int? maxWidth = null, 
            int? maxHeight = null)
        {
            string url = ApiUrl + "/video?id=" + itemId;

            url += "&outputformats=" + string.Join(",", supportedOutputFormats.Select(s => s.ToString()).ToArray());

            if (maxAudioChannels.HasValue)
            {
                url += "&audiochannels=" + maxAudioChannels.Value;
            }

            if (maxAudioSampleRate.HasValue)
            {
                url += "&audiosamplerate=" + maxAudioSampleRate.Value;
            }

            if (width.HasValue)
            {
                url += "&width=" + width.Value;
            }

            if (height.HasValue)
            {
                url += "&height=" + height.Value;
            }

            if (maxWidth.HasValue)
            {
                url += "&maxWidth=" + maxWidth.Value;
            }

            if (maxHeight.HasValue)
            {
                url += "&maxHeight=" + maxHeight.Value;
            }
            return url;
        }

        protected T DeserializeFromStream<T>(Stream stream)
            where T : class
        {
            return DataSerializer.DeserializeFromStream<T>(stream, SerializationFormat);
        }

        public virtual void Dispose()
        {
        }
    }
}
