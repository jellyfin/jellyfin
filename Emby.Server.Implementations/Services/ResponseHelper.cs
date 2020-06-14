#pragma warning disable CS1591

using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.HttpServer;
using MediaBrowser.Model.Services;
using Microsoft.AspNetCore.Http;

namespace Emby.Server.Implementations.Services
{
    public static class ResponseHelper
    {
        public static Task WriteToResponse(HttpResponse response, IRequest request, object result, CancellationToken cancellationToken)
        {
            if (result == null)
            {
                if (response.StatusCode == (int)HttpStatusCode.OK)
                {
                    response.StatusCode = (int)HttpStatusCode.NoContent;
                }

                response.ContentLength = 0;
                return Task.CompletedTask;
            }

            var httpResult = result as IHttpResult;
            if (httpResult != null)
            {
                httpResult.RequestContext = request;
                request.ResponseContentType = httpResult.ContentType ?? request.ResponseContentType;
            }

            var defaultContentType = request.ResponseContentType;

            if (httpResult != null)
            {
                if (httpResult.RequestContext == null)
                    httpResult.RequestContext = request;

                response.StatusCode = httpResult.Status;
            }

            if (result is IHasHeaders responseOptions)
            {
                foreach (var responseHeaders in responseOptions.Headers)
                {
                    if (string.Equals(responseHeaders.Key, "Content-Length", StringComparison.OrdinalIgnoreCase))
                    {
                        response.ContentLength = long.Parse(responseHeaders.Value, CultureInfo.InvariantCulture);
                        continue;
                    }

                    response.Headers.Add(responseHeaders.Key, responseHeaders.Value);
                }
            }

            //ContentType='text/html' is the default for a HttpResponse
            //Do not override if another has been set
            if (response.ContentType == null || response.ContentType == "text/html")
            {
                response.ContentType = defaultContentType;
            }

            if (response.ContentType == "application/json")
            {
                response.ContentType += "; charset=utf-8";
            }

            switch (result)
            {
                case IAsyncStreamWriter asyncStreamWriter:
                    return asyncStreamWriter.WriteToAsync(response.Body, cancellationToken);
                case IStreamWriter streamWriter:
                    streamWriter.WriteTo(response.Body);
                    return Task.CompletedTask;
                case FileWriter fileWriter:
                    return fileWriter.WriteToAsync(response, cancellationToken);
                case Stream stream:
                    return CopyStream(stream, response.Body);
                case byte[] bytes:
                    response.ContentType = "application/octet-stream";
                    response.ContentLength = bytes.Length;

                    if (bytes.Length > 0)
                    {
                        return response.Body.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
                    }

                    return Task.CompletedTask;
                case string responseText:
                    var responseTextAsBytes = Encoding.UTF8.GetBytes(responseText);
                    response.ContentLength = responseTextAsBytes.Length;

                    if (responseTextAsBytes.Length > 0)
                    {
                        return response.Body.WriteAsync(responseTextAsBytes, 0, responseTextAsBytes.Length, cancellationToken);
                    }

                    return Task.CompletedTask;
            }

            return WriteObject(request, result, response);
        }

        private static async Task CopyStream(Stream src, Stream dest)
        {
            using (src)
            {
                await src.CopyToAsync(dest).ConfigureAwait(false);
            }
        }

        public static async Task WriteObject(IRequest request, object result, HttpResponse response)
        {
            var contentType = request.ResponseContentType;
            var serializer = RequestHelper.GetResponseWriter(HttpListenerHost.Instance, contentType);

            using (var ms = new MemoryStream())
            {
                serializer(result, ms);

                ms.Position = 0;

                var contentLength = ms.Length;
                response.ContentLength = contentLength;

                if (contentLength > 0)
                {
                    await ms.CopyToAsync(response.Body).ConfigureAwait(false);
                }
            }
        }
    }
}
