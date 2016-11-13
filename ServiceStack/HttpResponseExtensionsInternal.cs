//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
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
                using (stream)
                {
                    await stream.CopyToAsync(response.OutputStream).ConfigureAwait(false);
                    return true;
                }
            }

            var bytes = result as byte[];
            if (bytes != null)
            {
                response.ContentType = "application/octet-stream";
                response.SetContentLength(bytes.Length);

                await response.OutputStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
                return true;
            }

            return false;
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

        public static Task WriteToResponse(this IResponse httpRes, IRequest httpReq, object result)
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
                return httpRes.WriteToResponseInternal(httpResult, httpReq);
            }

            return httpRes.WriteToResponseInternal(result, httpReq);
        }

        /// <summary>
        /// Writes to response.
        /// Response headers are customizable by implementing IHasHeaders an returning Dictionary of Http headers.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="result">Whether or not it was implicity handled by ServiceStack's built-in handlers.</param>
        /// <param name="request">The serialization context.</param>
        /// <returns></returns>
        private static async Task WriteToResponseInternal(this IResponse response, object result, IRequest request)
        {
            var defaultContentType = request.ResponseContentType;

            var httpResult = result as IHttpResult;
            if (httpResult != null)
            {
                if (httpResult.RequestContext == null)
                    httpResult.RequestContext = request;

                response.StatusCode = httpResult.Status;
                response.StatusDescription = httpResult.StatusCode.ToString();
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

            var responseOptions = result as IHasHeaders;
            if (responseOptions != null)
            {
                foreach (var responseHeaders in responseOptions.Headers)
                {
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

            var writeToOutputStreamResult = await WriteToOutputStream(response, result).ConfigureAwait(false);
            if (writeToOutputStreamResult)
            {
                return;
            }

            var responseText = result as string;
            if (responseText != null)
            {
                var bytes = Encoding.UTF8.GetBytes(responseText);
                response.SetContentLength(bytes.Length);
                await response.OutputStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
                return;
            }

            await WriteObject(request, result, response).ConfigureAwait(false);
        }

        public static async Task WriteObject(IRequest request, object result, IResponse response)
        {
            var contentType = request.ResponseContentType;
            var serializer = ContentTypes.GetStreamSerializer(contentType);
            
            using (var ms = new MemoryStream())
            {
                serializer(result, ms);

                ms.Position = 0;
                response.SetContentLength(ms.Length);
                await ms.CopyToAsync(response.OutputStream).ConfigureAwait(false);
            }

            //serializer(result, outputStream);
        }
    }
}
