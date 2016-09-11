//
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
//

using System;

namespace Mono.Nat.Pmp
{
    internal static class PmpConstants
	{
		public const byte Version = (byte)0;
		
		public const byte OperationCode = (byte)0;
		public const byte OperationCodeUdp = (byte)1;
		public const byte OperationCodeTcp = (byte)2;
        public const byte ServerNoop = (byte)128;
		
		public const int ClientPort = 5350;
		public const int ServerPort = 5351;
		
		public const int RetryDelay = 250;
		public const int RetryAttempts = 9;
		
		public const int RecommendedLeaseTime = 60 * 60;
		public const int DefaultLeaseTime = RecommendedLeaseTime;
		
		public const short ResultCodeSuccess = 0;
		public const short ResultCodeUnsupportedVersion = 1;
		public const short ResultCodeNotAuthorized = 2;
		public const short ResultCodeNetworkFailure = 3;
		public const short ResultCodeOutOfResources = 4;
		public const short ResultCodeUnsupportedOperationCode = 5;
	}
}