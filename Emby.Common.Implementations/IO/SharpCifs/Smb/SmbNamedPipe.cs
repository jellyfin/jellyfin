// This code is derived from jcifs smb client library <jcifs at samba dot org>
// Ported by J. Arturo <webmaster at komodosoft dot net>
//  
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
using System;
using SharpCifs.Util.Sharpen;

namespace SharpCifs.Smb
{
	/// <summary>
	/// This class will allow a Java program to read and write data to Named
	/// Pipes and Transact NamedPipes.
	/// </summary>
	/// <remarks>
	/// This class will allow a Java program to read and write data to Named
	/// Pipes and Transact NamedPipes.
	/// <p>There are three Win32 function calls provided by the Windows SDK
	/// that are important in the context of using jCIFS. They are:
	/// <ul>
	/// <li> <code>CallNamedPipe</code> A message-type pipe call that opens,
	/// writes to, reads from, and closes the pipe in a single operation.
	/// <li> <code>TransactNamedPipe</code> A message-type pipe call that
	/// writes to and reads from an existing pipe descriptor in one operation.
	/// <li> <code>CreateFile</code>, <code>ReadFile</code>,
	/// <code>WriteFile</code>, and <code>CloseFile</code> A byte-type pipe can
	/// be opened, written to, read from and closed using the standard Win32
	/// file operations.
	/// </ul>
	/// <p>The jCIFS API maps all of these operations into the standard Java
	/// <code>XxxputStream</code> interface. A special <code>PIPE_TYPE</code>
	/// flags is necessary to distinguish which type of Named Pipe behavior
	/// is desired.
	/// <p><table border="1" cellpadding="3" cellspacing="0" width="100%">
	/// <tr bgcolor="#ccccff">
	/// <td colspan="2"><b><code>SmbNamedPipe</code> Constructor Examples</b></td>
	/// <tr><td width="20%"><b>Code Sample</b></td><td><b>Description</b></td></tr>
	/// <tr><td width="20%"><pre>
	/// new SmbNamedPipe( "smb://server/IPC$/PIPE/foo",
	/// SmbNamedPipe.PIPE_TYPE_RDWR |
	/// SmbNamedPipe.PIPE_TYPE_CALL );
	/// </pre></td><td>
	/// Open the Named Pipe foo for reading and writing. The pipe will behave like the <code>CallNamedPipe</code> interface.
	/// </td></tr>
	/// <tr><td width="20%"><pre>
	/// new SmbNamedPipe( "smb://server/IPC$/foo",
	/// SmbNamedPipe.PIPE_TYPE_RDWR |
	/// SmbNamedPipe.PIPE_TYPE_TRANSACT );
	/// </pre></td><td>
	/// Open the Named Pipe foo for reading and writing. The pipe will behave like the <code>TransactNamedPipe</code> interface.
	/// </td></tr>
	/// <tr><td width="20%"><pre>
	/// new SmbNamedPipe( "smb://server/IPC$/foo",
	/// SmbNamedPipe.PIPE_TYPE_RDWR );
	/// </pre></td><td>
	/// Open the Named Pipe foo for reading and writing. The pipe will
	/// behave as though the <code>CreateFile</code>, <code>ReadFile</code>,
	/// <code>WriteFile</code>, and <code>CloseFile</code> interface was
	/// being used.
	/// </td></tr>
	/// </table>
	/// <p>See <a href="../../../pipes.html">Using jCIFS to Connect to Win32
	/// Named Pipes</a> for a detailed description of how to use jCIFS with
	/// Win32 Named Pipe server processes.
	/// </remarks>
	public class SmbNamedPipe : SmbFile
	{
		/// <summary>The pipe should be opened read-only.</summary>
		/// <remarks>The pipe should be opened read-only.</remarks>
		public const int PipeTypeRdonly = ORdonly;

		/// <summary>The pipe should be opened only for writing.</summary>
		/// <remarks>The pipe should be opened only for writing.</remarks>
		public const int PipeTypeWronly = OWronly;

		/// <summary>The pipe should be opened for both reading and writing.</summary>
		/// <remarks>The pipe should be opened for both reading and writing.</remarks>
		public const int PipeTypeRdwr = ORdwr;

		/// <summary>Pipe operations should behave like the <code>CallNamedPipe</code> Win32 Named Pipe function.
		/// 	</summary>
		/// <remarks>Pipe operations should behave like the <code>CallNamedPipe</code> Win32 Named Pipe function.
		/// 	</remarks>
		public const int PipeTypeCall = unchecked(0x0100);

		/// <summary>Pipe operations should behave like the <code>TransactNamedPipe</code> Win32 Named Pipe function.
		/// 	</summary>
		/// <remarks>Pipe operations should behave like the <code>TransactNamedPipe</code> Win32 Named Pipe function.
		/// 	</remarks>
		public const int PipeTypeTransact = unchecked(0x0200);

		public const int PipeTypeDceTransact = unchecked(0x0200) | unchecked(0x0400);

		internal InputStream PipeIn;

		internal OutputStream PipeOut;

		internal int PipeType;

		/// <summary>
		/// Open the Named Pipe resource specified by the url
		/// parameter.
		/// </summary>
		/// <remarks>
		/// Open the Named Pipe resource specified by the url
		/// parameter. The pipeType parameter should be at least one of
		/// the <code>PIPE_TYPE</code> flags combined with the bitwise OR
		/// operator <code>|</code>. See the examples listed above.
		/// </remarks>
		/// <exception cref="System.UriFormatException"></exception>
		/// <exception cref="UnknownHostException"></exception>
		public SmbNamedPipe(string url, int pipeType) : base(url)
		{
			this.PipeType = pipeType;
			Type = TypeNamedPipe;
		}

		/// <exception cref="System.UriFormatException"></exception>
		/// <exception cref="UnknownHostException"></exception>
		public SmbNamedPipe(string url, int pipeType, NtlmPasswordAuthentication auth) : 
			base(url, auth)
		{
			this.PipeType = pipeType;
			Type = TypeNamedPipe;
		}

		/// <exception cref="System.UriFormatException"></exception>
		/// <exception cref="UnknownHostException"></exception>
		public SmbNamedPipe(Uri url, int pipeType, NtlmPasswordAuthentication auth) : base
			(url, auth)
		{
			this.PipeType = pipeType;
			Type = TypeNamedPipe;
		}

		/// <summary>
		/// Return the <code>InputStream</code> used to read information
		/// from this pipe instance.
		/// </summary>
		/// <remarks>
		/// Return the <code>InputStream</code> used to read information
		/// from this pipe instance. Presumably data would first be written
		/// to the <code>OutputStream</code> associated with this Named
		/// Pipe instance although this is not a requirement (e.g. a
		/// read-only named pipe would write data to this stream on
		/// connection). Reading from this stream may block. Therefore it
		/// may be necessary that an addition thread be used to read and
		/// write to a Named Pipe.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual InputStream GetNamedPipeInputStream()
		{
			if (PipeIn == null)
			{
				if ((PipeType & PipeTypeCall) == PipeTypeCall || (PipeType & PipeTypeTransact
					) == PipeTypeTransact)
				{
					PipeIn = new TransactNamedPipeInputStream(this);
				}
				else
				{
					PipeIn = new SmbFileInputStream(this, (PipeType & unchecked((int)(0xFFFF00FF))) |
						 OExcl);
				}
			}
			return PipeIn;
		}

		/// <summary>
		/// Return the <code>OutputStream</code> used to write
		/// information to this pipe instance.
		/// </summary>
		/// <remarks>
		/// Return the <code>OutputStream</code> used to write
		/// information to this pipe instance. The act of writing data
		/// to this stream will result in response data recieved in the
		/// <code>InputStream</code> associated with this Named Pipe
		/// instance (unless of course it does not elicite a response or the pipe is write-only).
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual OutputStream GetNamedPipeOutputStream()
		{
			if (PipeOut == null)
			{
				if ((PipeType & PipeTypeCall) == PipeTypeCall || (PipeType & PipeTypeTransact
					) == PipeTypeTransact)
				{
					PipeOut = new TransactNamedPipeOutputStream(this);
				}
				else
				{
					PipeOut = new SmbFileOutputStream(this, false, (PipeType & unchecked((int)(0xFFFF00FF
						))) | OExcl);
				}
			}
			return PipeOut;
		}
	}
}
