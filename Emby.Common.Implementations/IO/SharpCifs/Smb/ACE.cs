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
using System.Text;
using SharpCifs.Util;

namespace SharpCifs.Smb
{
	/// <summary>
	/// An Access Control Entry (ACE) is an element in a security descriptor
	/// such as those associated with files and directories.
	/// </summary>
	/// <remarks>
	/// An Access Control Entry (ACE) is an element in a security descriptor
	/// such as those associated with files and directories. The Windows OS
	/// determines which users have the necessary permissions to access objects
	/// based on these entries.
	/// <p>
	/// To fully understand the information exposed by this class a description
	/// of the access check algorithm used by Windows is required. The following
	/// is a basic description of the algorithm. For a more complete description
	/// we recommend reading the section on Access Control in Keith Brown's
	/// "The .NET Developer's Guide to Windows Security" (which is also
	/// available online).
	/// <p>
	/// Direct ACEs are evaluated first in order. The SID of the user performing
	/// the operation and the desired access bits are compared to the SID
	/// and access mask of each ACE. If the SID matches, the allow/deny flags
	/// and access mask are considered. If the ACE is a "deny"
	/// ACE and <i>any</i> of the desired access bits match bits in the access
	/// mask of the ACE, the whole access check fails. If the ACE is an "allow"
	/// ACE and <i>all</i> of the bits in the desired access bits match bits in
	/// the access mask of the ACE, the access check is successful. Otherwise,
	/// more ACEs are evaluated until all desired access bits (combined)
	/// are "allowed". If all of the desired access bits are not "allowed"
	/// the then same process is repeated for inherited ACEs.
	/// <p>
	/// For example, if user <tt>WNET\alice</tt> tries to open a file
	/// with desired access bits <tt>0x00000003</tt> (<tt>FILE_READ_DATA |
	/// FILE_WRITE_DATA</tt>) and the target file has the following security
	/// descriptor ACEs:
	/// <pre>
	/// Allow WNET\alice     0x001200A9  Direct
	/// Allow Administrators 0x001F01FF  Inherited
	/// Allow SYSTEM         0x001F01FF  Inherited
	/// </pre>
	/// the access check would fail because the direct ACE has an access mask
	/// of <tt>0x001200A9</tt> which doesn't have the
	/// <tt>FILE_WRITE_DATA</tt> bit on (bit <tt>0x00000002</tt>). Actually, this isn't quite correct. If
	/// <tt>WNET\alice</tt> is in the local <tt>Administrators</tt> group the access check
	/// will succeed because the inherited ACE allows local <tt>Administrators</tt>
	/// both <tt>FILE_READ_DATA</tt> and <tt>FILE_WRITE_DATA</tt> access.
	/// </remarks>
	public class Ace
	{
		public const int FileReadData = unchecked(0x00000001);

		public const int FileWriteData = unchecked(0x00000002);

		public const int FileAppendData = unchecked(0x00000004);

		public const int FileReadEa = unchecked(0x00000008);

		public const int FileWriteEa = unchecked(0x00000010);

		public const int FileExecute = unchecked(0x00000020);

		public const int FileDelete = unchecked(0x00000040);

		public const int FileReadAttributes = unchecked(0x00000080);

		public const int FileWriteAttributes = unchecked(0x00000100);

		public const int Delete = unchecked(0x00010000);

		public const int ReadControl = unchecked(0x00020000);

		public const int WriteDac = unchecked(0x00040000);

		public const int WriteOwner = unchecked(0x00080000);

		public const int Synchronize = unchecked(0x00100000);

		public const int GenericAll = unchecked(0x10000000);

		public const int GenericExecute = unchecked(0x20000000);

		public const int GenericWrite = unchecked(0x40000000);

		public const int GenericRead = unchecked((int)(0x80000000));

		public const int FlagsObjectInherit = unchecked(0x01);

		public const int FlagsContainerInherit = unchecked(0x02);

		public const int FlagsNoPropagate = unchecked(0x04);

		public const int FlagsInheritOnly = unchecked(0x08);

		public const int FlagsInherited = unchecked(0x10);

		internal bool Allow;

		internal int Flags;

		internal int Access;

		internal Sid Sid;

		// 1
		// 2
		// 3
		// 4
		// 5
		// 6
		// 7
		// 8
		// 9
		// 16
		// 17
		// 18
		// 19
		// 20
		// 28
		// 29
		// 30
		// 31
		/// <summary>Returns true if this ACE is an allow ACE and false if it is a deny ACE.</summary>
		/// <remarks>Returns true if this ACE is an allow ACE and false if it is a deny ACE.</remarks>
		public virtual bool IsAllow()
		{
			return Allow;
		}

		/// <summary>Returns true if this ACE is an inherited ACE and false if it is a direct ACE.
		/// 	</summary>
		/// <remarks>
		/// Returns true if this ACE is an inherited ACE and false if it is a direct ACE.
		/// <p>
		/// Note: For reasons not fully understood, <tt>FLAGS_INHERITED</tt> may
		/// not be set within all security descriptors even though the ACE was in
		/// face inherited. If an inherited ACE is added to a parent the Windows
		/// ACL editor will rebuild all children ACEs and set this flag accordingly.
		/// </remarks>
		public virtual bool IsInherited()
		{
			return (Flags & FlagsInherited) != 0;
		}

		/// <summary>Returns the flags for this ACE.</summary>
		/// <remarks>
		/// Returns the flags for this ACE. The </tt>isInherited()</tt>
		/// method checks the <tt>FLAGS_INHERITED</tt> bit in these flags.
		/// </remarks>
		public virtual int GetFlags()
		{
			return Flags;
		}

		/// <summary>
		/// Returns the 'Apply To' text for inheritance of ACEs on
		/// directories such as 'This folder, subfolder and files'.
		/// </summary>
		/// <remarks>
		/// Returns the 'Apply To' text for inheritance of ACEs on
		/// directories such as 'This folder, subfolder and files'. For
		/// files the text is always 'This object only'.
		/// </remarks>
		public virtual string GetApplyToText()
		{
			switch (Flags & (FlagsObjectInherit | FlagsContainerInherit | FlagsInheritOnly
				))
			{
				case unchecked(0x00):
				{
					return "This folder only";
				}

				case unchecked(0x03):
				{
					return "This folder, subfolders and files";
				}

				case unchecked(0x0B):
				{
					return "Subfolders and files only";
				}

				case unchecked(0x02):
				{
					return "This folder and subfolders";
				}

				case unchecked(0x0A):
				{
					return "Subfolders only";
				}

				case unchecked(0x01):
				{
					return "This folder and files";
				}

				case unchecked(0x09):
				{
					return "Files only";
				}
			}
			return "Invalid";
		}

		/// <summary>Returns the access mask accociated with this ACE.</summary>
		/// <remarks>
		/// Returns the access mask accociated with this ACE. Use the
		/// constants for <tt>FILE_READ_DATA</tt>, <tt>FILE_WRITE_DATA</tt>,
		/// <tt>READ_CONTROL</tt>, <tt>GENERIC_ALL</tt>, etc with bitwise
		/// operators to determine which bits of the mask are on or off.
		/// </remarks>
		public virtual int GetAccessMask()
		{
			return Access;
		}

		/// <summary>Return the SID associated with this ACE.</summary>
		/// <remarks>Return the SID associated with this ACE.</remarks>
		public virtual Sid GetSid()
		{
			return Sid;
		}

		internal virtual int Decode(byte[] buf, int bi)
		{
			Allow = buf[bi++] == unchecked(unchecked(0x00));
			Flags = buf[bi++] & unchecked(0xFF);
			int size = ServerMessageBlock.ReadInt2(buf, bi);
			bi += 2;
			Access = ServerMessageBlock.ReadInt4(buf, bi);
			bi += 4;
			Sid = new Sid(buf, bi);
			return size;
		}

		internal virtual void AppendCol(StringBuilder sb, string str, int width)
		{
			sb.Append(str);
			int count = width - str.Length;
			for (int i = 0; i < count; i++)
			{
				sb.Append(' ');
			}
		}

		/// <summary>Return a string represeting this ACE.</summary>
		/// <remarks>
		/// Return a string represeting this ACE.
		/// <p>
		/// Note: This function should probably be changed to return SDDL
		/// fragments but currently it does not.
		/// </remarks>
		public override string ToString()
		{
			int count;
			int i;
			string str;
			StringBuilder sb = new StringBuilder();
			sb.Append(IsAllow() ? "Allow " : "Deny  ");
			AppendCol(sb, Sid.ToDisplayString(), 25);
			sb.Append(" 0x").Append(Hexdump.ToHexString(Access, 8)).Append(' ');
			sb.Append(IsInherited() ? "Inherited " : "Direct    ");
			AppendCol(sb, GetApplyToText(), 34);
			return sb.ToString();
		}
	}
}
