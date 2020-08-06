//
// Authors:
//   Alan McGovern <alan.mcgovern@gmail.com>
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

using System;
using System.Net;

namespace Mono.Nat.Pmp
{
	class ResponseMessage
	{
		public static ResponseMessage Decode (byte [] data)
		{
			if (data.Length < 16)
				throw new MappingException ($"The received message was too short, only {data.Length} bytes");

			if (data [0] != PmpConstants.Version)
				throw new MappingException ($"The received message was unsupported version {data [0]}");

			byte opCode = (byte) (data [1] & (byte) 127);

			Protocol protocol = Protocol.Tcp;
			if (opCode == PmpConstants.OperationCodeUdp)
				protocol = Protocol.Udp;

			var resultCode = (ErrorCode) IPAddress.NetworkToHostOrder (BitConverter.ToInt16 (data, 2));
			uint epoch = (uint) IPAddress.NetworkToHostOrder (BitConverter.ToInt32 (data, 4));

			int privatePort = IPAddress.NetworkToHostOrder (BitConverter.ToInt16 (data, 8));
			int publicPort = IPAddress.NetworkToHostOrder (BitConverter.ToInt16 (data, 10));

			uint lifetime = (uint) IPAddress.NetworkToHostOrder (BitConverter.ToInt32 (data, 12));

			if (publicPort < 0 || privatePort < 0 || resultCode != ErrorCode.Success)
				throw new MappingException ((ErrorCode) resultCode, "Could not modify the port map");

			var mapping = new Mapping (protocol, privatePort, publicPort, (int) lifetime, null);
			return new MappingResponseMessage (mapping);
		}
	}
}
