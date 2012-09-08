using MediaBrowser.Model.Authentication;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.DTO;
using MediaBrowser.Model.Weather;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace MediaBrowser.ApiInteraction.Portable
{
    public class ApiClient : BaseApiClient
    {
        private HttpWebRequest GetNewRequest(string url)
        {
            return HttpWebRequest.CreateHttp(url);
        }

        /// <summary>
        /// Gets an image stream based on a url
        /// </summary>
        public void GetImageStreamAsync(string url, Action<Stream> callback)
        {
            GetStreamAsync(url, callback);
        }

        /// <summary>
        /// Gets an image stream based on a url
        /// </summary>
        private void GetStreamAsync(string url, Action<Stream> callback)
        {
            HttpWebRequest request = GetNewRequest(url);

            request.BeginGetResponse(new AsyncCallback(result =>
            {
                using (WebResponse response = (result.AsyncState as HttpWebRequest).EndGetResponse(result))
                {
                    Stream stream = response.GetResponseStream();
                    callback(stream);
                }

            }), request);
        }

        /// <summary>
        /// Gets a BaseItem
        /// </summary>
        public void GetItemAsync(Guid id, Guid userId, Action<DTOBaseItem> callback)
        {
            string url = ApiUrl + "/item?userId=" + userId.ToString();

            if (id != Guid.Empty)
            {
                url += "&id=" + id.ToString();
            }

            GetDataAsync(url, callback);
        }

        /// <summary>
        /// Gets all users
        /// </summary>
        public void GetAllUsersAsync(Action<DTOUser[]> callback)
        {
            string url = ApiUrl + "/users";

            GetDataAsync(url, callback);
        }

        /// <summary>
        /// Gets all Genres
        /// </summary>
        public void GetAllGenresAsync(Guid userId, Action<IBNItem[]> callback)
        {
            string url = ApiUrl + "/genres?userId=" + userId.ToString();

            GetDataAsync(url, callback);
        }

        /// <summary>
        /// Gets in-progress items
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="folderId">(Optional) Specify a folder Id to localize the search to a specific folder.</param>
        public void GetInProgressItemsItemsAsync(Guid userId, Action<DTOBaseItem[]> callback, Guid? folderId = null)
        {
            string url = ApiUrl + "/itemlist?listtype=inprogressitems&userId=" + userId.ToString();

            if (folderId.HasValue)
            {
                url += "&id=" + folderId.ToString();
            }

            GetDataAsync(url, callback);
        }

        /// <summary>
        /// Gets recently added items
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="folderId">(Optional) Specify a folder Id to localize the search to a specific folder.</param>
        public void GetRecentlyAddedItemsAsync(Guid userId, Action<DTOBaseItem[]> callback, Guid? folderId = null)
        {
            string url = ApiUrl + "/itemlist?listtype=recentlyaddeditems&userId=" + userId.ToString();

            if (folderId.HasValue)
            {
                url += "&id=" + folderId.ToString();
            }

            GetDataAsync(url, callback);
        }

        /// <summary>
        /// Gets recently added items that are unplayed.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="folderId">(Optional) Specify a folder Id to localize the search to a specific folder.</param>
        public void GetRecentlyAddedUnplayedItemsAsync(Guid userId, Action<DTOBaseItem[]> callback, Guid? folderId = null)
        {
            string url = ApiUrl + "/itemlist?listtype=recentlyaddedunplayeditems&userId=" + userId.ToString();

            if (folderId.HasValue)
            {
                url += "&id=" + folderId.ToString();
            }

            GetDataAsync(url, callback);
        }

        /// <summary>
        /// Gets all Years
        /// </summary>
        public void GetAllYearsAsync(Guid userId, Action<IBNItem[]> callback)
        {
            string url = ApiUrl + "/years?userId=" + userId.ToString();

            GetDataAsync(url, callback);
        }

        /// <summary>
        /// Gets all items that contain a given Year
        /// </summary>
        public void GetItemsWithYearAsync(string name, Guid userId, Action<DTOBaseItem[]> callback)
        {
            string url = ApiUrl + "/itemlist?listtype=itemswithyear&userId=" + userId.ToString() + "&name=" + name;

            GetDataAsync(url, callback);
        }

        /// <summary>
        /// Gets all items that contain a given Genre
        /// </summary>
        public void GetItemsWithGenreAsync(string name, Guid userId, Action<DTOBaseItem[]> callback)
        {
            string url = ApiUrl + "/itemlist?listtype=itemswithgenre&userId=" + userId.ToString() + "&name=" + name;

            GetDataAsync(url, callback);
        }

        /// <summary>
        /// Gets all items that contain a given Person
        /// </summary>
        public void GetItemsWithPersonAsync(string name, Guid userId, Action<DTOBaseItem[]> callback)
        {
            string url = ApiUrl + "/itemlist?listtype=itemswithperson&userId=" + userId.ToString() + "&name=" + name;

            GetDataAsync(url, callback);
        }

        /// <summary>
        /// Gets all items that contain a given Person
        /// </summary>
        public void GetItemsWithPersonAsync(string name, string personType, Guid userId, Action<DTOBaseItem[]> callback)
        {
            string url = ApiUrl + "/itemlist?listtype=itemswithperson&userId=" + userId.ToString() + "&name=" + name;

            url += "&persontype=" + personType;

            GetDataAsync(url, callback);
        }

        /// <summary>
        /// Gets all studious
        /// </summary>
        public void GetAllStudiosAsync(Guid userId, Action<IBNItem[]> callback)
        {
            string url = ApiUrl + "/studios?userId=" + userId.ToString();

            GetDataAsync(url, callback);
        }

        /// <summary>
        /// Gets all items that contain a given Studio
        /// </summary>
        public void GetItemsWithStudioAsync(string name, Guid userId, Action<DTOBaseItem[]> callback)
        {
            string url = ApiUrl + "/itemlist?listtype=itemswithstudio&userId=" + userId.ToString() + "&name=" + name;

            GetDataAsync(url, callback);
        }

        /// <summary>
        /// Gets a studio
        /// </summary>
        public void GetStudioAsync(Guid userId, string name, Action<IBNItem> callback)
        {
            string url = ApiUrl + "/studio?userId=" + userId.ToString() + "&name=" + name;

            GetDataAsync(url, callback);
        }

        /// <summary>
        /// Gets a genre
        /// </summary>
        public void GetGenreAsync(Guid userId, string name, Action<IBNItem> callback)
        {
            string url = ApiUrl + "/genre?userId=" + userId.ToString() + "&name=" + name;

            GetDataAsync(url, callback);
        }

        /// <summary>
        /// Gets a person
        /// </summary>
        public void GetPersonAsync(Guid userId, string name, Action<IBNItem> callback)
        {
            string url = ApiUrl + "/person?userId=" + userId.ToString() + "&name=" + name;

            GetDataAsync(url, callback);
        }

        /// <summary>
        /// Gets a year
        /// </summary>
        public void GetYearAsync(Guid userId, int year, Action<IBNItem> callback)
        {
            string url = ApiUrl + "/year?userId=" + userId.ToString() + "&year=" + year;

            GetDataAsync(url, callback);
        }

        /// <summary>
        /// Gets a list of plugins installed on the server
        /// </summary>
        public void GetInstalledPluginsAsync(Action<PluginInfo[]> callback)
        {
            string url = ApiUrl + "/plugins";

            GetDataAsync(url, callback);
        }

        /// <summary>
        /// Gets a list of plugins installed on the server
        /// </summary>
        public void GetPluginAssemblyAsync(PluginInfo plugin, Action<Stream> callback)
        {
            string url = ApiUrl + "/pluginassembly?assemblyfilename=" + plugin.AssemblyFileName;

            GetStreamAsync(url, callback);
        }

        /// <summary>
        /// Gets the current server configuration
        /// </summary>
        public void GetServerConfigurationAsync(Action<ServerConfiguration> callback)
        {
            string url = ApiUrl + "/ServerConfiguration";

            GetDataAsync(url, callback);
        }

        /// <summary>
        /// Gets weather information for the default location as set in configuration
        /// </summary>
        public void GetPluginConfigurationAsync(PluginInfo plugin, Type configurationType, Action<object> callback)
        {
            string url = ApiUrl + "/PluginConfiguration?assemblyfilename=" + plugin.AssemblyFileName;

            // At the moment this can't be retrieved in protobuf format
            SerializationFormats format = DataSerializer.CanDeSerializeJsv ? SerializationFormats.Jsv : SerializationFormats.Json;

            GetDataAsync(url, callback, configurationType, format);
        }

        /// <summary>
        /// Gets the default user
        /// </summary>
        public void GetDefaultUserAsync(Action<DTOUser> callback)
        {
            string url = ApiUrl + "/user";

            GetDataAsync(url, callback);
        }

        /// <summary>
        /// Gets a user by id
        /// </summary>
        public void GetUserAsync(Guid id, Action<DTOUser> callback)
        {
            string url = ApiUrl + "/user?id=" + id.ToString();

            GetDataAsync(url, callback);
        }

        /// <summary>
        /// Gets weather information for the default location as set in configuration
        /// </summary>
        public void GetWeatherInfoAsync(Action<WeatherInfo> callback)
        {
            string url = ApiUrl + "/weather";

            GetDataAsync(url, callback);
        }

        /// <summary>
        /// Gets weather information for a specific zip code
        /// </summary>
        public void GetWeatherInfoAsync(string zipCode, Action<WeatherInfo> callback)
        {
            string url = ApiUrl + "/weather?zipcode=" + zipCode;

            GetDataAsync(url, callback);
        }

        /// <summary>
        /// Authenticates a user and returns the result
        /// </summary>
        public void AuthenticateUserAsync(Guid userId, string password, Action<AuthenticationResult> callback)
        {
            string url = ApiUrl + "/UserAuthentication?dataformat=" + SerializationFormat.ToString();

            Dictionary<string, string> formValues = new Dictionary<string, string>();

            formValues["userid"] = userId.ToString();

            if (!string.IsNullOrEmpty(password))
            {
                formValues["password"] = password;
            }

            PostDataAsync(url, formValues, callback, SerializationFormat);
        }

        /// <summary>
        /// Performs a GET request, and deserializes the response stream to an object of Type T
        /// </summary>
        private void GetDataAsync<T>(string url, Action<T> callback)
        {
            GetDataAsync<T>(url, callback, SerializationFormat);
        }

        /// <summary>
        /// Performs a GET request, and deserializes the response stream to an object of Type T
        /// </summary>
        private void GetDataAsync<T>(string url, Action<T> callback, SerializationFormats serializationFormat)
        {
            if (url.IndexOf('?') == -1)
            {
                url += "?dataformat=" + serializationFormat.ToString();
            }
            else
            {
                url += "&dataformat=" + serializationFormat.ToString();
            }

            HttpWebRequest request = GetNewRequest(url);

            request.BeginGetResponse(new AsyncCallback(result =>
            {
                T value;

                using (WebResponse response = (result.AsyncState as HttpWebRequest).EndGetResponse(result))
                {
                    using (Stream stream = response.GetResponseStream())
                    {
                        value = DeserializeFromStream<T>(stream);
                    }
                }

                callback(value);

            }), request);
        }

        /// <summary>
        /// Performs a GET request, and deserializes the response stream to an object of Type T
        /// </summary>
        private void GetDataAsync(string url, Action<object> callback, Type type, SerializationFormats serializationFormat)
        {
            if (url.IndexOf('?') == -1)
            {
                url += "?dataformat=" + serializationFormat.ToString();
            }
            else
            {
                url += "&dataformat=" + serializationFormat.ToString();
            }

            HttpWebRequest request = GetNewRequest(url);

            request.BeginGetResponse(new AsyncCallback(result =>
            {
                object value;

                using (WebResponse response = (result.AsyncState as HttpWebRequest).EndGetResponse(result))
                {
                    using (Stream stream = response.GetResponseStream())
                    {
                        value = DataSerializer.DeserializeFromStream(stream, serializationFormat, type);
                    }
                }

                callback(value);

            }), request);
        }

        /// <summary>
        /// Performs a POST request, and deserializes the response stream to an object of Type T
        /// </summary>
        private void PostDataAsync<T>(string url, Dictionary<string, string> formValues, Action<T> callback, SerializationFormats serializationFormat)
        {
            if (url.IndexOf('?') == -1)
            {
                url += "?dataformat=" + serializationFormat.ToString();
            }
            else
            {
                url += "&dataformat=" + serializationFormat.ToString();
            }

            HttpWebRequest request = GetNewRequest(url);

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";

            // Begin getting request stream
            request.BeginGetRequestStream(new AsyncCallback(beginGetRequestStreamResult =>
            {
                // Once we have the request stream, write the post data
                using (Stream requestStream = request.EndGetRequestStream(beginGetRequestStreamResult))
                {
                    // Construct the body
                    string postBody = string.Join("&", formValues.Keys.Select(s => string.Format("{0}={1}", s, formValues[s])).ToArray());
                    
                    // Convert the string into a byte array. 
                    byte[] byteArray = Encoding.UTF8.GetBytes(postBody);

                    // Write to the request stream.
                    requestStream.Write(byteArray, 0, byteArray.Length);
                }

                // Begin getting response stream
                request.BeginGetResponse(new AsyncCallback(result =>
                {
                    // Once we have it, deserialize the data and execute the callback
                    T value;

                    using (WebResponse response = request.EndGetResponse(result))
                    {
                        using (Stream responseStream = response.GetResponseStream())
                        {
                            value = DeserializeFromStream<T>(responseStream);
                        }
                    }

                    callback(value);

                }), null);

            }), null);
        }
    }
}
