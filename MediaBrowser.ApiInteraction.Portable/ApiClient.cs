using MediaBrowser.Model.DTO;
using System;
using System.IO;
using System.Net;

namespace MediaBrowser.ApiInteraction.Portable
{
    public class ApiClient : BaseApiClient
    {
        private HttpWebRequest GetNewRequest(string url)
        {
            return HttpWebRequest.CreateHttp(url);
        }

        /// <summary>
        /// Gets all users
        /// </summary>
        public void GetAllUsersAsync(Action<DTOUser[]> callback)
        {
            string url = ApiUrl + "/users";

            GetDataAsync<DTOUser[]>(url, callback);
        }

        private void GetDataAsync<T>(string url, Action<T> callback)
        {
            GetDataAsync<T>(url, callback, SerializationFormat);
        }

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
    }
}
