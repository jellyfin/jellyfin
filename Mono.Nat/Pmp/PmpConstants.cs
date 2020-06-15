// Authors:
//   Ben Motmans <ben.motmans@gmail.com>
//
// Copyright (C) 2007 Ben Motmans
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

namespace Mono.Nat.Pmp
{
    using System;

    /// <summary>
    /// Defines the <see cref="PmpConstants" />.
    /// </summary>
    internal static class PmpConstants
    {
        /// <summary>
        /// Defines the RetryAttempts.
        /// </summary>
        public const int RetryAttempts = 8;

        /// <summary>
        /// Defines the Version.
        /// </summary>
        public const byte Version = 0;

        /// <summary>
        /// Defines the OperationCode.
        /// </summary>
        public const byte OperationCode = 0;

        /// <summary>
        /// Defines the OperationCodeUdp.
        /// </summary>
        public const byte OperationCodeUdp = 1;

        /// <summary>
        /// Defines the OperationCodeTcp.
        /// </summary>
        public const byte OperationCodeTcp = 2;

        /// <summary>
        /// Defines the ServerNoop.
        /// </summary>
        public const byte ServerNoop = 128;

        /// <summary>
        /// Defines the ServerPort.
        /// </summary>
        public const int ServerPort = 5351;

        /// <summary>
        /// Defines the RetryDelay.
        /// </summary>
        public static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(250);

        /// <summary>
        /// Defines the RecommendedLeaseTime.
        /// </summary>
        public static readonly TimeSpan RecommendedLeaseTime = TimeSpan.FromHours(2);
    }
}
