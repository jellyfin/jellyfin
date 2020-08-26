//
// Authors:
//   Alan McGovern alan.mcgovern@gmail.com
//
// Copyright (C) 2019 Alan McGovern
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

namespace Mono.Nat
{
    public enum ErrorCode
    {
        #region uPnP error codes

        /// <summary>
        /// An unknown error occurred
        /// </summary>
        Unknown = -9999,

        /// <summary>
        /// One, or more, of the arguments were invalid.
        /// </summary>
        InvalidArgs = 402,

        /// <summary>
        /// Unexpected failure performing the request.
        /// </summary>
        ActionFailed = 501,

        /// <summary>
        /// The specified array index is out of bounds 
        /// </summary>
        SpecifiedArrayIndexInvalid = 713,

        /// <summary>
        /// The source IP address cannot be wild-carded.
        /// </summary>
        WildCardNotPermittedInSourceIP = 715,

        /// <summary>
        /// The external port cannot be wild-carded.
        /// </summary>
        WildCardNotPermittedInExternalPort = 716,

        /// <summary>
        /// The port mapping entry specified conflicts with a mapping assigned previously to another client.
        /// </summary>
        ConflictInMappingEntry = 718,

        /// <summary>
        /// Internal and External port values must be the same.
        /// </summary>
        SamePortValuesRequired = 724,

        #endregion uPnP error codes

        #region NAT-PMP error codes

        /// <summary>
        /// No error occurred.
        /// </summary>
        Success = 0,

        /// <summary>
        /// The NAT
        /// </summary>
        UnsupportedVersion = 1,

        /// <summary>
        /// The NAT device supports creating mappings, but the user has turned the feature off.
        /// </summary>
        NotAuthorizedOrRefused = 2,

        /// <summary>
        /// The NAT device itself has not obtained a DHCP lease.
        /// </summary>
        NetworkFailure = 3,

        /// <summary>
        /// The NAT device cannot create any more mappings at this time.
        /// </summary>
        OutOfResources = 4,

        /// <summary>
        /// The NAT device does not support this operation.
        /// </summary>
        UnsupportedOperation = 5,

        #endregion NAT-PMP error codes
    }
}
