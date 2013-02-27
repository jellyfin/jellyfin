using MediaBrowser.Model.Connectivity;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Web;
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
        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <value>The logger.</value>
        protected ILogger Logger { get; private set; }

        /// <summary>
        /// Gets the protobuf serializer.
        /// </summary>
        /// <value>The protobuf serializer.</value>
        public IProtobufSerializer ProtobufSerializer { get; set; }

        /// <summary>
        /// Gets the json serializer.
        /// </summary>
        /// <value>The json serializer.</value>
        public IJsonSerializer JsonSerializer { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseApiClient" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <exception cref="System.ArgumentNullException">logger</exception>
        protected BaseApiClient(ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            JsonSerializer = new NewtonsoftJsonSerializer();
            Logger = logger;
            SerializationFormat = SerializationFormats.Json;
        }

        /// <summary>
        /// Gets or sets the server host name (myserver or 192.168.x.x)
        /// </summary>
        /// <value>The name of the server host.</value>
        public string ServerHostName { get; set; }

        /// <summary>
        /// Gets or sets the port number used by the API
        /// </summary>
        /// <value>The server API port.</value>
        public int ServerApiPort { get; set; }

        /// <summary>
        /// Gets or sets the type of the client.
        /// </summary>
        /// <value>The type of the client.</value>
        public ClientType ClientType { get; set; }

        /// <summary>
        /// Gets or sets the name of the device.
        /// </summary>
        /// <value>The name of the device.</value>
        public string DeviceName { get; set; }

        private Guid? _currentUserId;

        /// <summary>
        /// Gets or sets the current user id.
        /// </summary>
        /// <value>The current user id.</value>
        public virtual Guid? CurrentUserId
        {
            get { return _currentUserId; }
            set
            {
                _currentUserId = value;
                ResetAuthorizationHeader();
            }
        }

        /// <summary>
        /// Gets the current api url based on hostname and port.
        /// </summary>
        /// <value>The API URL.</value>
        protected string ApiUrl
        {
            get
            {
                return string.Format("http://{0}:{1}/mediabrowser", ServerHostName, ServerApiPort);
            }
        }

        /// <summary>
        /// Gets the default data format to request from the server
        /// </summary>
        /// <value>The serialization format.</value>
        public SerializationFormats SerializationFormat { get; set; }

        /// <summary>
        /// Resets the authorization header.
        /// </summary>
        private void ResetAuthorizationHeader()
        {
            if (!CurrentUserId.HasValue)
            {
                SetAuthorizationHeader(null);
                return;
            }

            var header = string.Format("UserId=\"{0}\", Client=\"{1}\"", CurrentUserId.Value, ClientType);

            if (!string.IsNullOrEmpty(DeviceName))
            {
                header += string.Format(", Device=\"{0}\"", DeviceName);
            }

            SetAuthorizationHeader(header);
        }

        /// <summary>
        /// Sets the authorization header.
        /// </summary>
        /// <param name="header">The header.</param>
        protected abstract void SetAuthorizationHeader(string header);

        /// <summary>
        /// Gets the API URL.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">handler</exception>
        protected string GetApiUrl(string handler)
        {
            return GetApiUrl(handler, new QueryStringDictionary());
        }

        /// <summary>
        /// Gets the API URL.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="queryString">The query string.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">handler</exception>
        protected string GetApiUrl(string handler, QueryStringDictionary queryString)
        {
            if (string.IsNullOrEmpty(handler))
            {
                throw new ArgumentNullException("handler");
            }

            if (queryString == null)
            {
                throw new ArgumentNullException("queryString");
            }

            return queryString.GetUrl(ApiUrl + "/" + handler);
        }

        /// <summary>
        /// Creates a url to return a list of items
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="listType">The type of list to retrieve.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">query</exception>
        protected string GetItemListUrl(ItemQuery query, string listType = null)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            var dict = new QueryStringDictionary { };

            dict.AddIfNotNullOrEmpty("listtype", listType);
            dict.AddIfNotNullOrEmpty("ParentId", query.ParentId);

            dict.AddIfNotNull("startindex", query.StartIndex);

            dict.AddIfNotNull("limit", query.Limit);

            if (query.SortBy != null)
            {
                dict["sortBy"] = string.Join(",", query.SortBy.Select(s => s.ToString()));
            }

            if (query.SortOrder.HasValue)
            {
                dict["sortOrder"] = query.SortOrder.ToString();
            }

            if (query.Fields != null)
            {
                dict.Add("fields", query.Fields.Select(f => f.ToString()));
            }
            if (query.Filters != null)
            {
                dict.Add("Filters", query.Filters.Select(f => f.ToString()));
            }
            if (query.ImageTypes != null)
            {
                dict.Add("ImageTypes", query.ImageTypes.Select(f => f.ToString()));
            }

            dict.Add("recursive", query.Recursive);

            dict.AddIfNotNull("genres", query.Genres);
            dict.AddIfNotNull("studios", query.Studios);
            dict.AddIfNotNull("ExcludeItemTypes", query.ExcludeItemTypes);
            dict.AddIfNotNull("IncludeItemTypes", query.IncludeItemTypes);

            dict.AddIfNotNullOrEmpty("person", query.Person);
            dict.AddIfNotNullOrEmpty("personType", query.PersonType);

            dict.AddIfNotNull("years", query.Years);

            dict.AddIfNotNullOrEmpty("indexBy", query.IndexBy);
            dict.AddIfNotNullOrEmpty("dynamicSortBy", query.DynamicSortBy);
            dict.AddIfNotNullOrEmpty("SearchTerm", query.SearchTerm);

            return GetApiUrl("Users/" + query.UserId + "/Items", dict);
        }

        /// <summary>
        /// Gets the image URL.
        /// </summary>
        /// <param name="baseUrl">The base URL.</param>
        /// <param name="options">The options.</param>
        /// <param name="queryParams">The query params.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">options</exception>
        private string GetImageUrl(string baseUrl, ImageOptions options, QueryStringDictionary queryParams)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            if (queryParams == null)
            {
                throw new ArgumentNullException("queryParams");
            }

            if (options.ImageIndex.HasValue)
            {
                baseUrl += "/" + options.ImageIndex.Value;
            }

            queryParams.AddIfNotNull("width", options.Width);
            queryParams.AddIfNotNull("height", options.Height);
            queryParams.AddIfNotNull("maxWidth", options.MaxWidth);
            queryParams.AddIfNotNull("maxHeight", options.MaxHeight);
            queryParams.AddIfNotNull("Quality", options.Quality);

            queryParams.AddIfNotNull("tag", options.Tag);

            return GetApiUrl(baseUrl, queryParams);
        }

        /// <summary>
        /// Gets the image URL.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public string GetImageUrl(BaseItemDto item, ImageOptions options)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            var index = options.ImageIndex ?? 0;

            if (options.ImageType == ImageType.Backdrop)
            {
                options.Tag = item.BackdropImageTags[index];
            }
            else if (options.ImageType == ImageType.ChapterImage)
            {
                options.Tag = item.Chapters[index].ImageTag;
            }
            else
            {
                options.Tag = item.ImageTags[options.ImageType];
            }

            return GetImageUrl(item.Id, options);
        }

        /// <summary>
        /// Gets an image url that can be used to download an image from the api
        /// </summary>
        /// <param name="itemId">The Id of the item</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">itemId</exception>
        public string GetImageUrl(string itemId, ImageOptions options)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                throw new ArgumentNullException("itemId");
            }

            var url = "Items/" + itemId + "/Images/" + options.ImageType;

            return GetImageUrl(url, options, new QueryStringDictionary());
        }

        /// <summary>
        /// Gets the user image URL.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">user</exception>
        public string GetUserImageUrl(UserDto user, ImageOptions options)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            options.Tag = user.PrimaryImageTag;

            return GetUserImageUrl(user.Id, options);
        }

        /// <summary>
        /// Gets an image url that can be used to download an image from the api
        /// </summary>
        /// <param name="userId">The Id of the user</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">userId</exception>
        public string GetUserImageUrl(Guid userId, ImageOptions options)
        {
            if (userId == Guid.Empty)
            {
                throw new ArgumentNullException("userId");
            }

            var url = "Users/" + userId + "/Images/" + options.ImageType;

            return GetImageUrl(url, options, new QueryStringDictionary());
        }

        /// <summary>
        /// Gets the person image URL.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public string GetPersonImageUrl(BaseItemPerson item, ImageOptions options)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            options.Tag = item.PrimaryImageTag;

            return GetPersonImageUrl(item.Name, options);
        }

        /// <summary>
        /// Gets the person image URL.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public string GetPersonImageUrl(BaseItemDto item, ImageOptions options)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            options.Tag = item.ImageTags[ImageType.Primary];

            return GetPersonImageUrl(item.Name, options);
        }

        /// <summary>
        /// Gets an image url that can be used to download an image from the api
        /// </summary>
        /// <param name="name">The name of the person</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">name</exception>
        public string GetPersonImageUrl(string name, ImageOptions options)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            var url = "Persons/" + name + "/Images/" + options.ImageType;

            return GetImageUrl(url, options, new QueryStringDictionary());
        }

        /// <summary>
        /// Gets the year image URL.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public string GetYearImageUrl(BaseItemDto item, ImageOptions options)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            options.Tag = item.ImageTags[ImageType.Primary];

            return GetYearImageUrl(int.Parse(item.Name), options);
        }

        /// <summary>
        /// Gets an image url that can be used to download an image from the api
        /// </summary>
        /// <param name="year">The year.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        public string GetYearImageUrl(int year, ImageOptions options)
        {
            var url = "Years/" + year + "/Images/" + options.ImageType;

            return GetImageUrl(url, options, new QueryStringDictionary());
        }

        /// <summary>
        /// Gets the genre image URL.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public string GetGenreImageUrl(BaseItemDto item, ImageOptions options)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            options.Tag = item.ImageTags[ImageType.Primary];

            return GetGenreImageUrl(item.Name, options);
        }

        /// <summary>
        /// Gets an image url that can be used to download an image from the api
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">name</exception>
        public string GetGenreImageUrl(string name, ImageOptions options)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            var url = "Genres/" + name + "/Images/" + options.ImageType;

            return GetImageUrl(url, options, new QueryStringDictionary());
        }

        /// <summary>
        /// Gets the studio image URL.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public string GetStudioImageUrl(BaseItemDto item, ImageOptions options)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            options.Tag = item.ImageTags[ImageType.Primary];

            return GetStudioImageUrl(item.Name, options);
        }

        /// <summary>
        /// Gets an image url that can be used to download an image from the api
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">name</exception>
        public string GetStudioImageUrl(string name, ImageOptions options)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            var url = "Studios/" + name + "/Images/" + options.ImageType;

            return GetImageUrl(url, options, new QueryStringDictionary());
        }

        /// <summary>
        /// This is a helper to get a list of backdrop url's from a given ApiBaseItemWrapper. If the actual item does not have any backdrops it will return backdrops from the first parent that does.
        /// </summary>
        /// <param name="item">A given item.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String[][].</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public string[] GetBackdropImageUrls(BaseItemDto item, ImageOptions options)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            options.ImageType = ImageType.Backdrop;

            string backdropItemId;
            List<Guid> backdropImageTags;

            if (item.BackdropCount == 0)
            {
                backdropItemId = item.ParentBackdropItemId;
                backdropImageTags = item.ParentBackdropImageTags;
            }
            else
            {
                backdropItemId = item.Id;
                backdropImageTags = item.BackdropImageTags;
            }

            if (string.IsNullOrEmpty(backdropItemId))
            {
                return new string[] { };
            }

            var files = new string[backdropImageTags.Count];

            for (var i = 0; i < backdropImageTags.Count; i++)
            {
                options.ImageIndex = i;
                options.Tag = backdropImageTags[i];

                files[i] = GetImageUrl(backdropItemId, options);
            }

            return files;
        }

        /// <summary>
        /// This is a helper to get the logo image url from a given ApiBaseItemWrapper. If the actual item does not have a logo, it will return the logo from the first parent that does, or null.
        /// </summary>
        /// <param name="item">A given item.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public string GetLogoImageUrl(BaseItemDto item, ImageOptions options)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            options.ImageType = ImageType.Logo;

            var logoItemId = item.HasLogo ? item.Id : item.ParentLogoItemId;
            var imageTag = item.HasLogo ? item.ImageTags[ImageType.Logo] : item.ParentLogoImageTag;

            if (!string.IsNullOrEmpty(logoItemId))
            {
                options.Tag = imageTag;

                return GetImageUrl(logoItemId, options);
            }

            return null;
        }

        /// <summary>
        /// Gets the url needed to stream an audio file
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">options</exception>
        public string GetAudioStreamUrl(StreamOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            var handler = "Audio/" + options.ItemId + "/stream";

            if (!string.IsNullOrEmpty(options.OutputFileExtension))
            {
                handler += "." + options.OutputFileExtension.TrimStart('.');
            }

            return GetMediaStreamUrl(handler, options, new QueryStringDictionary());
        }

        /// <summary>
        /// Gets the url needed to stream a video file
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">options</exception>
        public string GetVideoStreamUrl(VideoStreamOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            var handler = "Videos/" + options.ItemId + "/stream";

            if (!string.IsNullOrEmpty(options.OutputFileExtension))
            {
                handler += "." + options.OutputFileExtension.TrimStart('.');
            }

            return GetVideoStreamUrl(handler, options);
        }

        /// <summary>
        /// Formulates a url for streaming audio using the HLS protocol
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">options</exception>
        public string GetHlsAudioStreamUrl(StreamOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            return GetMediaStreamUrl("audio.m3u8", options, new QueryStringDictionary());
        }

        /// <summary>
        /// Formulates a url for streaming video using the HLS protocol
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">options</exception>
        public string GetHlsVideoStreamUrl(VideoStreamOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            return GetVideoStreamUrl("video.m3u8", options);
        }

        /// <summary>
        /// Gets the video stream URL.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        private string GetVideoStreamUrl(string handler, VideoStreamOptions options)
        {
            var queryParams = new QueryStringDictionary();

            if (options.VideoCodec.HasValue)
            {
                queryParams["VideoCodec"] = options.VideoCodec.Value.ToString();
            }

            queryParams.AddIfNotNull("VideoBitRate", options.VideoBitRate);
            queryParams.AddIfNotNull("Width", options.Width);
            queryParams.AddIfNotNull("Height", options.Height);
            queryParams.AddIfNotNull("MaxWidth", options.MaxWidth);
            queryParams.AddIfNotNull("MaxHeight", options.MaxHeight);
            queryParams.AddIfNotNull("FrameRate", options.FrameRate);
            queryParams.AddIfNotNull("AudioStreamIndex", options.AudioStreamIndex);
            queryParams.AddIfNotNull("VideoStreamIndex", options.VideoStreamIndex);
            queryParams.AddIfNotNull("SubtitleStreamIndex", options.SubtitleStreamIndex);

            return GetMediaStreamUrl(handler, options, queryParams);
        }

        /// <summary>
        /// Gets the media stream URL.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="options">The options.</param>
        /// <param name="queryParams">The query params.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">handler</exception>
        private string GetMediaStreamUrl(string handler, StreamOptions options, QueryStringDictionary queryParams)
        {
            if (string.IsNullOrEmpty(handler))
            {
                throw new ArgumentNullException("handler");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            if (queryParams == null)
            {
                throw new ArgumentNullException("queryParams");
            }

            if (options.AudioCodec.HasValue)
            {
                queryParams["audioCodec"] = options.AudioCodec.Value.ToString();
            }

            queryParams.AddIfNotNull("audiochannels", options.MaxAudioChannels);
            queryParams.AddIfNotNull("audiosamplerate", options.MaxAudioSampleRate);
            queryParams.AddIfNotNull("AudioBitRate", options.AudioBitRate);
            queryParams.AddIfNotNull("StartTimeTicks", options.StartTimeTicks);
            queryParams.AddIfNotNull("Static", options.Static);

            return GetApiUrl(handler, queryParams);
        }

        /// <summary>
        /// Deserializes from stream.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="stream">The stream.</param>
        /// <returns>``0.</returns>
        protected T DeserializeFromStream<T>(Stream stream)
            where T : class
        {
            return (T)DeserializeFromStream(stream, typeof(T), SerializationFormat);
        }

        /// <summary>
        /// Deserializes from stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="type">The type.</param>
        /// <param name="format">The format.</param>
        /// <returns>System.Object.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        protected object DeserializeFromStream(Stream stream, Type type, SerializationFormats format)
        {
            if (format == SerializationFormats.Protobuf)
            {
                return ProtobufSerializer.DeserializeFromStream(stream, type);
            }
            if (format == SerializationFormats.Json)
            {
                return JsonSerializer.DeserializeFromStream(stream, type);
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// Serializers to json.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.String.</returns>
        protected string SerializeToJson(object obj)
        {
            return JsonSerializer.SerializeToString(obj);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {

        }
    }
}
