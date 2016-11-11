//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using MediaBrowser.Model.Services;
using ServiceStack.Host;

namespace ServiceStack
{
    public static class HttpResponseExtensionsInternal
    {
        public static async Task<bool> WriteToOutputStream(IResponse response, object result)
        {
            var asyncStreamWriter = result as IAsyncStreamWriter;
            if (asyncStreamWriter != null)
            {
                await asyncStreamWriter.WriteToAsync(response.OutputStream, CancellationToken.None).ConfigureAwait(false);
                return true;
            }

            var streamWriter = result as IStreamWriter;
            if (streamWriter != null)
            {
                streamWriter.WriteTo(response.OutputStream);
                return true;
            }

            var stream = result as Stream;
            if (stream != null)
            {
                WriteTo(stream, response.OutputStream);
                return true;
            }

            var bytes = result as byte[];
            if (bytes != null)
            {
                response.ContentType = "application/octet-stream";
                response.SetContentLength(bytes.Length);

                response.OutputStream.Write(bytes, 0, bytes.Length);
                return true;
            }

            return false;
        }

        public static long WriteTo(Stream inStream, Stream outStream)
        {
            var memoryStream = inStream as MemoryStream;
            if (memoryStream != null)
            {
                memoryStream.WriteTo(outStream);
                return memoryStream.Position;
            }

            var data = new byte[4096];
            long total = 0;
            int bytesRead;

            while ((bytesRead = inStream.Read(data, 0, data.Length)) > 0)
            {
                outStream.Write(data, 0, bytesRead);
                total += bytesRead;
            }

            return total;
        }

        /// <summary>
        /// End a ServiceStack Request with no content
        /// </summary>
        public static void EndRequestWithNoContent(this IResponse httpRes)
        {
            if (httpRes.StatusCode == (int)HttpStatusCode.OK)
            {
                httpRes.StatusCode = (int)HttpStatusCode.NoContent;
            }

            httpRes.SetContentLength(0);
        }

        public static Task WriteToResponse(this IResponse httpRes, MediaBrowser.Model.Services.IRequest httpReq, object result)
        {
            if (result == null)
            {
                httpRes.EndRequestWithNoContent();
                return Task.FromResult(true);
            }

            var httpResult = result as IHttpResult;
            if (httpResult != null)
            {
                httpResult.RequestContext = httpReq;
                httpReq.ResponseContentType = httpResult.ContentType ?? httpReq.ResponseContentType;
                var httpResSerializer = ContentTypes.Instance.GetResponseSerializer(httpReq.ResponseContentType);
                return httpRes.WriteToResponse(httpResult, httpResSerializer, httpReq);
            }

            var serializer = ContentTypes.Instance.GetResponseSerializer(httpReq.ResponseContentType);
            return httpRes.WriteToResponse(result, serializer, httpReq);
        }

        private static object GetDto(object response)
        {
            if (response == null) return null;
            var httpResult = response as IHttpResult;
            return httpResult != null ? httpResult.Response : response;
        }

        /// <summary>
        /// Writes to response.
        /// Response headers are customizable by implementing IHasHeaders an returning Dictionary of Http headers.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="result">Whether or not it was implicity handled by ServiceStack's built-in handlers.</param>
        /// <param name="defaultAction">The default action.</param>
        /// <param name="request">The serialization context.</param>
        /// <returns></returns>
        public static async Task WriteToResponse(this IResponse response, object result, Action<IRequest, object, IResponse> defaultAction, MediaBrowser.Model.Services.IRequest request)
        {
            var defaultContentType = request.ResponseContentType;
            if (result == null)
            {
                response.EndRequestWithNoContent();
                return;
            }

            var httpResult = result as IHttpResult;
            if (httpResult != null)
            {
                if (httpResult.RequestContext == null)
                    httpResult.RequestContext = request;

                response.Dto = response.Dto ?? GetDto(httpResult);

                response.StatusCode = httpResult.Status;
                response.StatusDescription = httpResult.StatusDescription ?? httpResult.StatusCode.ToString();
                if (string.IsNullOrEmpty(httpResult.ContentType))
                {
                    httpResult.ContentType = defaultContentType;
                }
                response.ContentType = httpResult.ContentType;

                if (httpResult.Cookies != null)
                {
                    var httpRes = response as IHttpResponse;
                    if (httpRes != null)
                    {
                        foreach (var cookie in httpResult.Cookies)
                        {
                            httpRes.SetCookie(cookie);
                        }
                    }
                }
            }
            else
            {
                response.Dto = result;
            }

            /* Mono Error: Exception: Method not found: 'System.Web.HttpResponse.get_Headers' */
            var responseOptions = result as IHasHeaders;
            if (responseOptions != null)
            {
                //Reserving options with keys in the format 'xx.xxx' (No Http headers contain a '.' so its a safe restriction)
                const string reservedOptions = ".";

                foreach (var responseHeaders in responseOptions.Headers)
                {
                    if (responseHeaders.Key.Contains(reservedOptions)) continue;
                    if (responseHeaders.Key == "Content-Length")
                    {
                        response.SetContentLength(long.Parse(responseHeaders.Value));
                        continue;
                    }

                    response.AddHeader(responseHeaders.Key, responseHeaders.Value);
                }
            }

            //ContentType='text/html' is the default for a HttpResponse
            //Do not override if another has been set
            if (response.ContentType == null || response.ContentType == "text/html")
            {
                response.ContentType = defaultContentType;
            }

            if (new HashSet<string> { "application/json", }.Contains(response.ContentType))
            {
                response.ContentType += "; charset=utf-8";
            }

            var disposableResult = result as IDisposable;
            var writeToOutputStreamResult = await WriteToOutputStream(response, result).ConfigureAwait(false);
            if (writeToOutputStreamResult)
            {
                response.Flush(); //required for Compression
                if (disposableResult != null) disposableResult.Dispose();
                return;
            }

            if (httpResult != null)
                result = httpResult.Response;

            var responseText = result as string;
            if (responseText != null)
            {
                if (response.ContentType == null || response.ContentType == "text/html")
                    response.ContentType = defaultContentType;
                response.Write(responseText);

                return;
            }

            if (defaultAction == null)
            {
                throw new ArgumentNullException("defaultAction", String.Format(
                    "As result '{0}' is not a supported responseType, a defaultAction must be supplied",
                    (result != null ? result.GetType().GetOperationName() : "")));
            }


            if (result != null)
                defaultAction(request, result, response);

            if (disposableResult != null)
                disposableResult.Dispose();
        }

    }
}
