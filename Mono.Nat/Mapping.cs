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

namespace Mono.Nat
{
	public class Mapping
	{
        private string description;
        private DateTime expiration;
        private int lifetime;
        private int privatePort;
		private Protocol protocol;
		private int publicPort;
		


		public Mapping (Protocol protocol, int privatePort, int publicPort)
			: this (protocol, privatePort, publicPort, 0)
		{
		}
		
		public Mapping (Protocol protocol, int privatePort, int publicPort, int lifetime)
		{
			this.protocol = protocol;
			this.privatePort = privatePort;
			this.publicPort = publicPort;
			this.lifetime = lifetime;

			if (lifetime == int.MaxValue)
				this.expiration = DateTime.MaxValue;
			else if (lifetime == 0)
				this.expiration = DateTime.Now;
			else
				this.expiration = DateTime.Now.AddSeconds (lifetime);
		}

        public string Description
        {
            get { return description; }
            set { description = value; }
        }
		
		public Protocol Protocol
		{
			get { return protocol; }
			internal set { protocol = value; }
		}

		public int PrivatePort
		{
			get { return privatePort; }
			internal set { privatePort = value; }
		}
		
		public int PublicPort
		{
			get { return publicPort; }
			internal set { publicPort = value; }
		}
		
		public int Lifetime
		{
			get { return lifetime; }
			internal set { lifetime = value; }
		}
		
		public DateTime Expiration
		{
			get { return expiration; }
			internal set { expiration = value; }
		}
		
		public bool IsExpired ()
		{
			return expiration < DateTime.Now;
		}

		public override bool Equals (object obj)
		{
			Mapping other = obj as Mapping;
			return other == null ? false : this.protocol == other.protocol &&
				this.privatePort == other.privatePort && this.publicPort == other.publicPort;
		}

		public override int GetHashCode()
		{
			return this.protocol.GetHashCode() ^ this.privatePort.GetHashCode() ^ this.publicPort.GetHashCode();
		}

        public override string ToString( )
        {
            return String.Format( "Protocol: {0}, Public Port: {1}, Private Port: {2}, Description: {3}, Expiration: {4}, Lifetime: {5}", 
                this.protocol, this.publicPort, this.privatePort, this.description, this.expiration, this.lifetime );
        }
	}
}
