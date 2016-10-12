/* This file is part of OpenSubtitles Handler
   A library that handle OpenSubtitles.org XML-RPC methods.

   Copyright © Ala Ibrahim Hadid 2013

   This program is free software: you can redistribute it and/or modify
   it under the terms of the GNU General Public License as published by
   the Free Software Foundation, either version 3 of the License, or
   (at your option) any later version.

   This program is distributed in the hope that it will be useful,
   but WITHOUT ANY WARRANTY; without even the implied warranty of
   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
   GNU General Public License for more details.

   You should have received a copy of the GNU General Public License
   along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;

namespace OpenSubtitlesHandler
{
    /// <summary>
    /// Include helper methods. All member are statics.
    /// </summary>
    public sealed class Utilities
    {
        private const string XML_RPC_SERVER = "https://api.opensubtitles.org/xml-rpc";

        /// <summary>
        /// Compute movie hash
        /// </summary>
        /// <param name="fileName">The complete media file path</param>
        /// <returns>The hash as Hexadecimal string</returns>
        public static string ComputeHash(string fileName)
        {
			byte[] hash = MovieHasher.ComputeMovieHash(File.OpenRead(fileName));
            return MovieHasher.ToHexadecimal(hash);
        }
        /// <summary>
        /// Compute md5 for a file
        /// </summary>
        /// <param name="filename">The complete file path</param>
        /// <returns>MD5 of the file</returns>
        public static string ComputeMd5(string filename)
        {
            var md5 = MD5.Create();
            var sb = new StringBuilder();
            Stream str = new FileStream(filename, FileMode.Open, FileAccess.Read);

            foreach (var b in md5.ComputeHash(str))
                sb.Append(b.ToString("x2").ToLower());
            str.Close();
            return sb.ToString();
        }
        /// <summary>
        /// Decompress data using GZip
        /// </summary>
        /// <param name="dataToDecompress">The stream that hold the data</param>
        /// <returns>Bytes array of decompressed data</returns>
        public static byte[] Decompress(Stream dataToDecompress)
        {
            MemoryStream target = new MemoryStream();

            using (System.IO.Compression.GZipStream decompressionStream = new System.IO.Compression.GZipStream(dataToDecompress,
                System.IO.Compression.CompressionMode.Decompress))
            {
                decompressionStream.CopyTo(target);
            }
            return target.GetBuffer();

        }

        /// <summary>
        /// Compress data using GZip (the retunred buffer will be WITHOUT HEADER)
        /// </summary>
        /// <param name="dataToCompress">The stream that hold the data</param>
        /// <returns>Bytes array of compressed data WITHOUT HEADER bytes</returns>
        public static byte[] Compress(Stream dataToCompress)
        {
            /*using (var compressed = new MemoryStream())
            {
                using (var compressor = new System.IO.Compression.GZipStream(compressed,
                    System.IO.Compression.CompressionMode.Compress))
                {
                    dataToCompress.CopyTo(compressor);
                }
                // Get the compressed bytes only after closing the GZipStream
                return compressed.ToArray();
            }*/
            //using (var compressedOutput = new MemoryStream())
            //{
            //    using (var compressedStream = new ZlibStream(compressedOutput,
            //        Ionic.Zlib.CompressionMode.Compress,
            //        CompressionLevel.Default, false))
            //    {
            //        var buffer = new byte[4096];
            //        int byteCount;
            //        do
            //        {
            //            byteCount = dataToCompress.Read(buffer, 0, buffer.Length);

            //            if (byteCount > 0)
            //            {
            //                compressedStream.Write(buffer, 0, byteCount);
            //            }
            //        } while (byteCount > 0);
            //    }
            //    return compressedOutput.ToArray();
            //}

            throw new NotImplementedException();
        }

        /// <summary>
        /// Handle server response stream and decode it as given encoding string.
        /// </summary>
        /// <param name="responseStream">The response stream. Expects a stream that doesn't support seek.</param>
        /// <param name="encoding">The encoding that should be used to decode buffer</param>
        /// <returns>The string of the stream after decode using given encoding</returns>
        public static string GetStreamString(Stream responseStream, Encoding encoding)
        {
            // Handle response, should be XML text.
            List<byte> data = new List<byte>();
            while (true)
            {
                int r = responseStream.ReadByte();
                if (r < 0)
                    break;
                data.Add((byte)r);
            }
            responseStream.Close();
            return encoding.GetString(data.ToArray());
        }
        /// <summary>
        /// Handle server response stream and decode it as ASCII encoding string.
        /// </summary>
        /// <param name="responseStream">The response stream. Expects a stream that doesn't support seek.</param>
        /// <returns>The string of the stream after decode using ASCII encoding</returns>
        public static string GetStreamString(Stream responseStream)
        {
            return GetStreamString(responseStream, Encoding.ASCII);
        }

        public static IHttpClient HttpClient { get; set; }

        /// <summary>
        /// Send a request to the server
        /// </summary>
        /// <param name="request">The request buffer to send as bytes array.</param>
        /// <param name="userAgent">The user agent value.</param>
        /// <returns>Response of the server or stream of error message as string started with 'ERROR:' keyword.</returns>
        public static Stream SendRequest(byte[] request, string userAgent)
        {
            return SendRequestAsync(request, userAgent, CancellationToken.None).Result;

            //HttpWebRequest req = (HttpWebRequest)WebRequest.Create(XML_RPC_SERVER);
            //req.ContentType = "text/xml";
            //req.Host = "api.opensubtitles.org:80";
            //req.Method = "POST";
            //req.UserAgent = "xmlrpc-epi-php/0.2 (PHP)";
            //req.ContentLength = request.Length;
            //ServicePointManager.Expect100Continue = false;
            //try
            //{
            //    using (Stream stm = req.GetRequestStream())
            //    {
            //        stm.Write(request, 0, request.Length);
            //    }

            //    WebResponse response = req.GetResponse();
            //    return response.GetResponseStream();
            //}
            //catch (Exception ex)
            //{
            //    Stream errorStream = new MemoryStream();
            //    byte[] dd = Encoding.ASCII.GetBytes("ERROR: " + ex.Message);
            //    errorStream.Write(dd, 0, dd.Length);
            //    errorStream.Position = 0;
            //    return errorStream;
            //}
        }

        public static async Task<Stream> SendRequestAsync(byte[] request, string userAgent, CancellationToken cancellationToken)
        {
            var options = new HttpRequestOptions
            {
                RequestContentBytes = request,
                RequestContentType = "text/xml",
                UserAgent = userAgent,
                Host = "api.opensubtitles.org:443",
                Url = XML_RPC_SERVER,

                // Response parsing will fail with this enabled
                EnableHttpCompression = false,

                CancellationToken = cancellationToken,
                BufferContent = false
            };

            if (string.IsNullOrEmpty(options.UserAgent))
            {
                options.UserAgent = "xmlrpc-epi-php/0.2 (PHP)";
            }

            var result = await HttpClient.Post(options).ConfigureAwait(false);

            return result.Content;

            //HttpWebRequest req = (HttpWebRequest)WebRequest.Create(XML_RPC_SERVER);
            //req.ContentType = "text/xml";
            //req.Host = "api.opensubtitles.org:80";
            //req.Method = "POST";
            //req.UserAgent = "xmlrpc-epi-php/0.2 (PHP)";
            //req.ContentLength = request.Length;
            //ServicePointManager.Expect100Continue = false;
            //try
            //{
            //    using (Stream stm = req.GetRequestStream())
            //    {
            //        stm.Write(request, 0, request.Length);
            //    }

            //    WebResponse response = req.GetResponse();
            //    return response.GetResponseStream();
            //}
            //catch (Exception ex)
            //{
            //    Stream errorStream = new MemoryStream();
            //    byte[] dd = Encoding.ASCII.GetBytes("ERROR: " + ex.Message);
            //    errorStream.Write(dd, 0, dd.Length);
            //    errorStream.Position = 0;
            //    return errorStream;
            //}
        }

    }
}
