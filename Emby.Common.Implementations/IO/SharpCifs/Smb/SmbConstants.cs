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
using System.Collections.Generic;
using System.Net;
using SharpCifs.Util.Sharpen;

namespace SharpCifs.Smb
{
    internal static class SmbConstants
    {
        public static readonly int DefaultPort = 445;

        public static readonly int DefaultMaxMpxCount = 10;

        public static readonly int DefaultResponseTimeout = 30000;

        public static readonly int DefaultSoTimeout = 35000;

        public static readonly int DefaultRcvBufSize = 60416;

        public static readonly int DefaultSndBufSize = 16644;

        public static readonly int DefaultSsnLimit = 250;

        public static readonly int DefaultConnTimeout = 35000;

        public static readonly IPAddress Laddr = Config.GetLocalHost();

        public static readonly int Lport = Config.GetInt("jcifs.smb.client.lport", 0);

        public static readonly int MaxMpxCount = Config.GetInt("jcifs.smb.client.maxMpxCount", DefaultMaxMpxCount
            );

        public static readonly int SndBufSize = Config.GetInt("jcifs.smb.client.snd_buf_size", DefaultSndBufSize
            );

        public static readonly int RcvBufSize = Config.GetInt("jcifs.smb.client.rcv_buf_size", DefaultRcvBufSize
            );

        public static readonly bool UseUnicode = Config.GetBoolean("jcifs.smb.client.useUnicode",
            true);

        public static readonly bool ForceUnicode = Config.GetBoolean("jcifs.smb.client.useUnicode"
            , false);

        public static readonly bool UseNtstatus = Config.GetBoolean("jcifs.smb.client.useNtStatus"
            , true);

        public static readonly bool Signpref = Config.GetBoolean("jcifs.smb.client.signingPreferred"
            , false);

        public static readonly bool UseNtsmbs = Config.GetBoolean("jcifs.smb.client.useNTSmbs", true
            );

        public static readonly bool UseExtsec = Config.GetBoolean("jcifs.smb.client.useExtendedSecurity"
            , true);

        public static readonly string NetbiosHostname = Config.GetProperty("jcifs.netbios.hostname"
            , null);

        public static readonly int LmCompatibility = Config.GetInt("jcifs.smb.lmCompatibility", 3);

        public static readonly int FlagsNone = unchecked(0x00);

        public static readonly int FlagsLockAndReadWriteAndUnlock = unchecked(0x01);

        public static readonly int FlagsReceiveBufferPosted = unchecked(0x02);

        public static readonly int FlagsPathNamesCaseless = unchecked(0x08);

        public static readonly int FlagsPathNamesCanonicalized = unchecked(0x10);

        public static readonly int FlagsOplockRequestedOrGranted = unchecked(0x20);

        public static readonly int FlagsNotifyOfModifyAction = unchecked(0x40);

        public static readonly int FlagsResponse = unchecked(0x80);

        public static readonly int Flags2None = unchecked(0x0000);

        public static readonly int Flags2LongFilenames = unchecked(0x0001);

        public static readonly int Flags2ExtendedAttributes = unchecked(0x0002);

        public static readonly int Flags2SecuritySignatures = unchecked(0x0004);

        public static readonly int Flags2ExtendedSecurityNegotiation = unchecked(0x0800);

        public static readonly int Flags2ResolvePathsInDfs = unchecked(0x1000);

        public static readonly int Flags2PermitReadIfExecutePerm = unchecked(0x2000);

        public static readonly int Flags2Status32 = unchecked(0x4000);

        public static readonly int Flags2Unicode = unchecked(0x8000);

        public static readonly int CapNone = unchecked(0x0000);

        public static readonly int CapRawMode = unchecked(0x0001);

        public static readonly int CapMpxMode = unchecked(0x0002);

        public static readonly int CapUnicode = unchecked(0x0004);

        public static readonly int CapLargeFiles = unchecked(0x0008);

        public static readonly int CapNtSmbs = unchecked(0x0010);

        public static readonly int CapRpcRemoteApis = unchecked(0x0020);

        public static readonly int CapStatus32 = unchecked(0x0040);

        public static readonly int CapLevelIiOplocks = unchecked(0x0080);

        public static readonly int CapLockAndRead = unchecked(0x0100);

        public static readonly int CapNtFind = unchecked(0x0200);

        public static readonly int CapDfs = unchecked(0x1000);

        public static readonly int CapExtendedSecurity = unchecked((int)(0x80000000));

        public static readonly int AttrReadonly = unchecked(0x01);

        public static readonly int AttrHidden = unchecked(0x02);

        public static readonly int AttrSystem = unchecked(0x04);

        public static readonly int AttrVolume = unchecked(0x08);

        public static readonly int AttrDirectory = unchecked(0x10);

        public static readonly int AttrArchive = unchecked(0x20);

        public static readonly int AttrCompressed = unchecked(0x800);

        public static readonly int AttrNormal = unchecked(0x080);

        public static readonly int AttrTemporary = unchecked(0x100);

        public static readonly int FileReadData = unchecked(0x00000001);

        public static readonly int FileWriteData = unchecked(0x00000002);

        public static readonly int FileAppendData = unchecked(0x00000004);

        public static readonly int FileReadEa = unchecked(0x00000008);

        public static readonly int FileWriteEa = unchecked(0x00000010);

        public static readonly int FileExecute = unchecked(0x00000020);

        public static readonly int FileDelete = unchecked(0x00000040);

        public static readonly int FileReadAttributes = unchecked(0x00000080);

        public static readonly int FileWriteAttributes = unchecked(0x00000100);

        public static readonly int Delete = unchecked(0x00010000);

        public static readonly int ReadControl = unchecked(0x00020000);

        public static readonly int WriteDac = unchecked(0x00040000);

        public static readonly int WriteOwner = unchecked(0x00080000);

        public static readonly int Synchronize = unchecked(0x00100000);

        public static readonly int GenericAll = unchecked(0x10000000);

        public static readonly int GenericExecute = unchecked(0x20000000);

        public static readonly int GenericWrite = unchecked(0x40000000);

        public static readonly int GenericRead = unchecked((int)(0x80000000));

        public static readonly int FlagsTargetMustBeFile = unchecked(0x0001);

        public static readonly int FlagsTargetMustBeDirectory = unchecked(0x0002);

        public static readonly int FlagsCopyTargetModeAscii = unchecked(0x0004);

        public static readonly int FlagsCopySourceModeAscii = unchecked(0x0008);

        public static readonly int FlagsVerifyAllWrites = unchecked(0x0010);

        public static readonly int FlagsTreeCopy = unchecked(0x0020);

        public static readonly int OpenFunctionFailIfExists = unchecked(0x0000);

        public static readonly int OpenFunctionOverwriteIfExists = unchecked(0x0020);

        public static readonly int Pid = (int)(new Random().NextDouble() * 65536d);

        public static readonly int SecurityShare = unchecked(0x00);

        public static readonly int SecurityUser = unchecked(0x01);

        public static readonly int CmdOffset = 4;

        public static readonly int ErrorCodeOffset = 5;

        public static readonly int FlagsOffset = 9;

        public static readonly int SignatureOffset = 14;

        public static readonly int TidOffset = 24;

        public static readonly int HeaderLength = 32;

        public static readonly long MillisecondsBetween1970And1601 = 11644473600000L;

        public static readonly TimeZoneInfo Tz = TimeZoneInfo.Local;

        public static readonly bool UseBatching = Config.GetBoolean("jcifs.smb.client.useBatching"
            , true);

        public static readonly string OemEncoding = Config.GetProperty("jcifs.encoding", Config.DefaultOemEncoding
            );

        public static readonly string UniEncoding = "UTF-16LE";

        public static readonly int DefaultFlags2 = Flags2LongFilenames | Flags2ExtendedAttributes
             | (UseExtsec ? Flags2ExtendedSecurityNegotiation : 0) | (Signpref ? Flags2SecuritySignatures
             : 0) | (UseNtstatus ? Flags2Status32 : 0) | (UseUnicode ? Flags2Unicode : 0
            );

        public static readonly int DefaultCapabilities = (UseNtsmbs ? CapNtSmbs : 0) | (UseNtstatus
             ? CapStatus32 : 0) | (UseUnicode ? CapUnicode : 0) | CapDfs;

        public static readonly int Flags2 = Config.GetInt("jcifs.smb.client.flags2", DefaultFlags2
            );

        public static readonly int Capabilities = Config.GetInt("jcifs.smb.client.capabilities", DefaultCapabilities
            );

        public static readonly bool TcpNodelay = Config.GetBoolean("jcifs.smb.client.tcpNoDelay",
            false);

        public static readonly int ResponseTimeout = Config.GetInt("jcifs.smb.client.responseTimeout"
            , DefaultResponseTimeout);

        public static readonly List<SmbTransport> Connections = new List<SmbTransport>();

        public static readonly int SsnLimit = Config.GetInt("jcifs.smb.client.ssnLimit", DefaultSsnLimit
            );

        public static readonly int SoTimeout = Config.GetInt("jcifs.smb.client.soTimeout", DefaultSoTimeout
            );

        public static readonly int ConnTimeout = Config.GetInt("jcifs.smb.client.connTimeout", DefaultConnTimeout
            );

        public static readonly string NativeOs = Config.GetProperty("jcifs.smb.client.nativeOs", Runtime
            .GetProperty("os.name"));

        public static readonly string NativeLanman = Config.GetProperty("jcifs.smb.client.nativeLanMan"
            , "jCIFS");

        public static readonly int VcNumber = 1;

        public static SmbTransport NullTransport = new SmbTransport(null, 0, null, 0);
        // file attribute encoding
        // extended file attribute encoding(others same as above)
        // access mask encoding
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
        // flags for move and copy
        // open function
    }
}
