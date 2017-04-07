// 
// Exceptions.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;

namespace SharpCifs.Util.Sharpen
{
	public class VirtualMachineError : Error
	{
	}

	public class StackOverflowError : VirtualMachineError
	{
	}

	public class BrokenBarrierException : Exception
	{
	}

	internal class BufferUnderflowException : Exception
	{
	}

	public class CharacterCodingException : Exception
	{
	}

	public class DataFormatException : Exception
	{
	}

	public class EofException : Exception
	{
		public EofException ()
		{
		}

		public EofException (string msg) : base(msg)
		{
		}
	}

	public class Error : Exception
	{
		public Error ()
		{
		}

		public Error (Exception ex) : base("Runtime Exception", ex)
		{
		}

		public Error (string msg) : base(msg)
		{
		}

		public Error (string msg, Exception ex) : base(msg, ex)
		{
		}
	}

	public class ExecutionException : Exception
	{
		public ExecutionException (Exception inner): base ("Execution failed", inner)
		{
		}
	}

	public class InstantiationException : Exception
	{
	}

	public class InterruptedIoException : Exception
	{
		public InterruptedIoException (string msg) : base(msg)
		{
		}
	}

	public class MissingResourceException : Exception
	{
	}

	public class NoSuchAlgorithmException : Exception
	{
	}

	public class NoSuchElementException : Exception
	{
	}

	internal class NoSuchMethodException : Exception
	{
	}

	internal class OverlappingFileLockException : Exception
	{
	}

	public class ParseException : Exception
	{
		public ParseException ()
		{
		}

		public ParseException (string msg, int errorOffset) : base(string.Format ("Msg: {0}. Error Offset: {1}", msg, errorOffset))
		{ 
		}
	}

	public class RuntimeException : Exception
	{
		public RuntimeException ()
		{
		}

		public RuntimeException (Exception ex) : base("Runtime Exception", ex)
		{
		}

		public RuntimeException (string msg) : base(msg)
		{
		}

		public RuntimeException (string msg, Exception ex) : base(msg, ex)
		{
		}
	}

	internal class StringIndexOutOfBoundsException : Exception
	{
	}

	public class UnknownHostException : Exception
	{
		public UnknownHostException ()
		{
		}

	    public UnknownHostException(string message) : base(message)
	    {
	        
	    }

        public UnknownHostException (Exception ex): base ("Host not found", ex)
		{
		}
	}

	public class UnsupportedEncodingException : Exception
	{
	}

	internal class UriSyntaxException : Exception
	{
		public UriSyntaxException (string s, string msg) : base(s + " " + msg)
		{
		}
	}

	internal class ZipException : Exception
	{
	}

	public class GitException : Exception
	{
	}
	
	public class ConnectException: Exception
	{
		public ConnectException (string msg): base (msg)
		{
		}
	}
	
	class KeyManagementException: Exception
	{
	}
	
	class IllegalCharsetNameException: Exception
	{
		public IllegalCharsetNameException (string msg): base (msg)
		{
		}
	}
	
	class UnsupportedCharsetException: Exception
	{
		public UnsupportedCharsetException (string msg): base (msg)
		{
		}
	}
}

