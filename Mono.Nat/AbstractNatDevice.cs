//
// Authors:
//   Alan McGovern alan.mcgovern@gmail.com
//   Ben Motmans <ben.motmans@gmail.com>
//
// Copyright (C) 2006 Alan McGovern
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
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace Mono.Nat
{
	public abstract class AbstractNatDevice : INatDevice
	{
		private DateTime lastSeen;
		
		protected AbstractNatDevice ()
		{

		}

		public abstract IPAddress LocalAddress { get; }
		
		public DateTime LastSeen
		{
			get { return lastSeen; }
			set { lastSeen = value; }
		}

		public virtual void CreatePortMap (Mapping mapping)
		{
			IAsyncResult result = BeginCreatePortMap (mapping, null, null);
		    EndCreatePortMap(result);
		}

		public virtual void DeletePortMap (Mapping mapping)
		{
			IAsyncResult result = BeginDeletePortMap (mapping, null, mapping);
			EndDeletePortMap(result);
		}

		public virtual Mapping[] GetAllMappings ()
		{
			IAsyncResult result = BeginGetAllMappings (null, null);
			return EndGetAllMappings (result);
		}

		public virtual IPAddress GetExternalIP ()
		{
			IAsyncResult result = BeginGetExternalIP(null, null);
			return EndGetExternalIP(result);
		}

		public virtual Mapping GetSpecificMapping (Protocol protocol, int port)
		{
			IAsyncResult result = this.BeginGetSpecificMapping (protocol, port, null, null);
			return this.EndGetSpecificMapping(result);
		}

        public abstract IAsyncResult BeginCreatePortMap(Mapping mapping, AsyncCallback callback, object asyncState);
		public abstract IAsyncResult BeginDeletePortMap (Mapping mapping, AsyncCallback callback, object asyncState);
		
		public abstract IAsyncResult BeginGetAllMappings (AsyncCallback callback, object asyncState);
		public abstract IAsyncResult BeginGetExternalIP (AsyncCallback callback, object asyncState);
        public abstract IAsyncResult BeginGetSpecificMapping(Protocol protocol, int externalPort, AsyncCallback callback, object asyncState);

		public abstract void EndCreatePortMap (IAsyncResult result);
		public abstract void EndDeletePortMap (IAsyncResult result);
		
		public abstract Mapping[] EndGetAllMappings (IAsyncResult result);
		public abstract IPAddress EndGetExternalIP (IAsyncResult result);
		public abstract Mapping EndGetSpecificMapping (IAsyncResult result);
	}
}
