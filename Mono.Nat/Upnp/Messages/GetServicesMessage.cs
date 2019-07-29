//
// Authors:
//   Alan McGovern alan.mcgovern@gmail.com
//
// Copyright (C) 2006 Alan McGovern
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Net;
using MediaBrowser.Common.Net;

namespace Mono.Nat.Upnp
{
    internal class GetServicesMessage : MessageBase
    {
        private string _servicesDescriptionUrl;
        private EndPoint _hostAddress;

        public GetServicesMessage(string description, EndPoint hostAddress)
            : base(null)
        {
            if (string.IsNullOrEmpty(description))
            {
                throw new ArgumentException("Description is null/empty", nameof(description));
            }

            this._servicesDescriptionUrl = description;
            this._hostAddress = hostAddress ?? throw new ArgumentNullException(nameof(hostAddress));
        }

        public override string Method => "GET";

        public override HttpRequestOptions Encode()
        {
            var req = new HttpRequestOptions()
            {
                Url = $"http://{this._hostAddress}{this._servicesDescriptionUrl}"
            };

            req.RequestHeaders.Add("ACCEPT-LANGUAGE", "en");

            return req;
        }
    }
}
