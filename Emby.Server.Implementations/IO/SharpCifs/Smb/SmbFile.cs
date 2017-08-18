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
using System.IO;
using System.Net;
using System.Text;
using SharpCifs.Dcerpc;
using SharpCifs.Dcerpc.Msrpc;
using SharpCifs.Netbios;
using SharpCifs.Util;
using SharpCifs.Util.Sharpen;

namespace SharpCifs.Smb
{
    /// <summary>This class represents a resource on an SMB network.</summary>
    /// <remarks>
    /// This class represents a resource on an SMB network. Mainly these
    /// resources are files and directories however an <code>SmbFile</code>
    /// may also refer to servers and workgroups. If the resource is a file or
    /// directory the methods of <code>SmbFile</code> follow the behavior of
    /// the well known
    /// <see cref="FilePath">Sharpen.FilePath</see>
    /// class. One fundamental difference
    /// is the usage of a URL scheme [1] to specify the target file or
    /// directory. SmbFile URLs have the following syntax:
    /// <blockquote><pre>
    /// smb://[[[domain;]username[:password]@]server[:port]/[[share/[dir/]file]]][?[param=value[param2=value2[...]]]
    /// </pre></blockquote>
    /// This example:
    /// <blockquote><pre>
    /// smb://storage15/public/foo.txt
    /// </pre></blockquote>
    /// would reference the file <code>foo.txt</code> in the share
    /// <code>public</code> on the server <code>storage15</code>. In addition
    /// to referencing files and directories, jCIFS can also address servers,
    /// and workgroups.
    /// <p>
    /// <font color="#800000"><i>Important: all SMB URLs that represent
    /// workgroups, servers, shares, or directories require a trailing slash '/'.
    /// </i></font>
    /// <p>
    /// When using the <tt>java.net.URL</tt> class with
    /// 'smb://' URLs it is necessary to first call the static
    /// <tt>jcifs.Config.registerSmbURLHandler();</tt> method. This is required
    /// to register the SMB protocol handler.
    /// <p>
    /// The userinfo component of the SMB URL (<tt>domain;user:pass</tt>) must
    /// be URL encoded if it contains reserved characters. According to RFC 2396
    /// these characters are non US-ASCII characters and most meta characters
    /// however jCIFS will work correctly with anything but '@' which is used
    /// to delimit the userinfo component from the server and '%' which is the
    /// URL escape character itself.
    /// <p>
    /// The server
    /// component may a traditional NetBIOS name, a DNS name, or IP
    /// address. These name resolution mechanisms and their resolution order
    /// can be changed (See <a href="../../../resolver.html">Setting Name
    /// Resolution Properties</a>). The servername and path components are
    /// not case sensitive but the domain, username, and password components
    /// are. It is also likely that properties must be specified for jcifs
    /// to function (See <a href="../../overview-summary.html#scp">Setting
    /// JCIFS Properties</a>). Here are some examples of SMB URLs with brief
    /// descriptions of what they do:
    /// <p>[1] This URL scheme is based largely on the <i>SMB
    /// Filesharing URL Scheme</i> IETF draft.
    /// <p><table border="1" cellpadding="3" cellspacing="0" width="100%">
    /// <tr bgcolor="#ccccff">
    /// <td colspan="2"><b>SMB URL Examples</b></td>
    /// <tr><td width="20%"><b>URL</b></td><td><b>Description</b></td></tr>
    /// <tr><td width="20%"><code>smb://users-nyc;miallen:mypass@angus/tmp/</code></td><td>
    /// This URL references a share called <code>tmp</code> on the server
    /// <code>angus</code> as user <code>miallen</code> who's password is
    /// <code>mypass</code>.
    /// </td></tr>
    /// <tr><td width="20%">
    /// <code>smb://Administrator:P%40ss@msmith1/c/WINDOWS/Desktop/foo.txt</code></td><td>
    /// A relativly sophisticated example that references a file
    /// <code>msmith1</code>'s desktop as user <code>Administrator</code>. Notice the '@' is URL encoded with the '%40' hexcode escape.
    /// </td></tr>
    /// <tr><td width="20%"><code>smb://angus/</code></td><td>
    /// This references only a server. The behavior of some methods is different
    /// in this context(e.g. you cannot <code>delete</code> a server) however
    /// as you might expect the <code>list</code> method will list the available
    /// shares on this server.
    /// </td></tr>
    /// <tr><td width="20%"><code>smb://myworkgroup/</code></td><td>
    /// This syntactically is identical to the above example. However if
    /// <code>myworkgroup</code> happends to be a workgroup(which is indeed
    /// suggested by the name) the <code>list</code> method will return
    /// a list of servers that have registered themselves as members of
    /// <code>myworkgroup</code>.
    /// </td></tr>
    /// <tr><td width="20%"><code>smb://</code></td><td>
    /// Just as <code>smb://server/</code> lists shares and
    /// <code>smb://workgroup/</code> lists servers, the <code>smb://</code>
    /// URL lists all available workgroups on a netbios LAN. Again,
    /// in this context many methods are not valid and return default
    /// values(e.g. <code>isHidden</code> will always return false).
    /// </td></tr>
    /// <tr><td width="20%"><code>smb://angus.foo.net/d/jcifs/pipes.doc</code></td><td>
    /// The server name may also be a DNS name as it is in this example. See
    /// <a href="../../../resolver.html">Setting Name Resolution Properties</a>
    /// for details.
    /// </td></tr>
    /// <tr><td width="20%"><code>smb://192.168.1.15/ADMIN$/</code></td><td>
    /// The server name may also be an IP address. See &lt;a
    /// href="../../../resolver.html"&gt;Setting Name Resolution Properties</a>
    /// for details.
    /// </td></tr>
    /// <tr><td width="20%">
    /// <code>smb://domain;username:password@server/share/path/to/file.txt</code></td><td>
    /// A prototypical example that uses all the fields.
    /// </td></tr>
    /// <tr><td width="20%"><code>smb://myworkgroup/angus/ &lt;-- ILLEGAL </code></td><td>
    /// Despite the hierarchial relationship between workgroups, servers, and
    /// filesystems this example is not valid.
    /// </td></tr>
    /// <tr><td width="20%">
    /// <code>smb://server/share/path/to/dir &lt;-- ILLEGAL </code></td><td>
    /// URLs that represent workgroups, servers, shares, or directories require a trailing slash '/'.
    /// </td></tr>
    /// <tr><td width="20%">
    /// <code>smb://MYGROUP/?SERVER=192.168.10.15</code></td><td>
    /// SMB URLs support some query string parameters. In this example
    /// the <code>SERVER</code> parameter is used to override the
    /// server name service lookup to contact the server 192.168.10.15
    /// (presumably known to be a master
    /// browser) for the server list in workgroup <code>MYGROUP</code>.
    /// </td></tr>
    /// </table>
    /// <p>A second constructor argument may be specified to augment the URL
    /// for better programmatic control when processing many files under
    /// a common base. This is slightly different from the corresponding
    /// <code>java.io.File</code> usage; a '/' at the beginning of the second
    /// parameter will still use the server component of the first parameter. The
    /// examples below illustrate the resulting URLs when this second contructor
    /// argument is used.
    /// <p><table border="1" cellpadding="3" cellspacing="0" width="100%">
    /// <tr bgcolor="#ccccff">
    /// <td colspan="3">
    /// <b>Examples Of SMB URLs When Augmented With A Second Constructor Parameter</b></td>
    /// <tr><td width="20%">
    /// <b>First Parameter</b></td><td><b>Second Parameter</b></td><td><b>Result</b></td></tr>
    /// <tr><td width="20%"><code>
    /// smb://host/share/a/b/
    /// </code></td><td width="20%"><code>
    /// c/d/
    /// </code></td><td><code>
    /// smb://host/share/a/b/c/d/
    /// </code></td></tr>
    /// <tr><td width="20%"><code>
    /// smb://host/share/foo/bar/
    /// </code></td><td width="20%"><code>
    /// /share2/zig/zag
    /// </code></td><td><code>
    /// smb://host/share2/zig/zag
    /// </code></td></tr>
    /// <tr><td width="20%"><code>
    /// smb://host/share/foo/bar/
    /// </code></td><td width="20%"><code>
    /// ../zip/
    /// </code></td><td><code>
    /// smb://host/share/foo/zip/
    /// </code></td></tr>
    /// <tr><td width="20%"><code>
    /// smb://host/share/zig/zag
    /// </code></td><td width="20%"><code>
    /// smb://foo/bar/
    /// </code></td><td><code>
    /// smb://foo/bar/
    /// </code></td></tr>
    /// <tr><td width="20%"><code>
    /// smb://host/share/foo/
    /// </code></td><td width="20%"><code>
    /// ../.././.././../foo/
    /// </code></td><td><code>
    /// smb://host/foo/
    /// </code></td></tr>
    /// <tr><td width="20%"><code>
    /// smb://host/share/zig/zag
    /// </code></td><td width="20%"><code>
    /// /
    /// </code></td><td><code>
    /// smb://host/
    /// </code></td></tr>
    /// <tr><td width="20%"><code>
    /// smb://server/
    /// </code></td><td width="20%"><code>
    /// ../
    /// </code></td><td><code>
    /// smb://server/
    /// </code></td></tr>
    /// <tr><td width="20%"><code>
    /// smb://
    /// </code></td><td width="20%"><code>
    /// myworkgroup/
    /// </code></td><td><code>
    /// smb://myworkgroup/
    /// </code></td></tr>
    /// <tr><td width="20%"><code>
    /// smb://myworkgroup/
    /// </code></td><td width="20%"><code>
    /// angus/
    /// </code></td><td><code>
    /// smb://myworkgroup/angus/ &lt;-- ILLEGAL<br />(But if you first create an <tt>SmbFile</tt> with 'smb://workgroup/' and use and use it as the first parameter to a constructor that accepts it with a second <tt>String</tt> parameter jCIFS will factor out the 'workgroup'.)
    /// </code></td></tr>
    /// </table>
    /// <p>Instances of the <code>SmbFile</code> class are immutable; that is,
    /// once created, the abstract pathname represented by an SmbFile object
    /// will never change.
    /// </remarks>
    /// <seealso cref="FilePath">Sharpen.FilePath</seealso>
    public class SmbFile : UrlConnection
    {
        internal const int ORdonly = 0x01;

        internal const int OWronly = 0x02;

        internal const int ORdwr = 0x03;

        internal const int OAppend = 0x04;

        internal const int OCreat = 0x0010;

        internal const int OExcl = 0x0020;

        internal const int OTrunc = 0x0040;

        /// <summary>
        /// When specified as the <tt>shareAccess</tt> constructor parameter,
        /// other SMB clients (including other threads making calls into jCIFS)
        /// will not be permitted to access the target file and will receive "The
        /// file is being accessed by another process" message.
        /// </summary>
        /// <remarks>
        /// When specified as the <tt>shareAccess</tt> constructor parameter,
        /// other SMB clients (including other threads making calls into jCIFS)
        /// will not be permitted to access the target file and will receive "The
        /// file is being accessed by another process" message.
        /// </remarks>
        public const int FileNoShare = 0x00;

        /// <summary>
        /// When specified as the <tt>shareAccess</tt> constructor parameter,
        /// other SMB clients will be permitted to read from the target file while
        /// this file is open.
        /// </summary>
        /// <remarks>
        /// When specified as the <tt>shareAccess</tt> constructor parameter,
        /// other SMB clients will be permitted to read from the target file while
        /// this file is open. This constant may be logically OR'd with other share
        /// access flags.
        /// </remarks>
        public const int FileShareRead = 0x01;

        /// <summary>
        /// When specified as the <tt>shareAccess</tt> constructor parameter,
        /// other SMB clients will be permitted to write to the target file while
        /// this file is open.
        /// </summary>
        /// <remarks>
        /// When specified as the <tt>shareAccess</tt> constructor parameter,
        /// other SMB clients will be permitted to write to the target file while
        /// this file is open. This constant may be logically OR'd with other share
        /// access flags.
        /// </remarks>
        public const int FileShareWrite = 0x02;

        /// <summary>
        /// When specified as the <tt>shareAccess</tt> constructor parameter,
        /// other SMB clients will be permitted to delete the target file while
        /// this file is open.
        /// </summary>
        /// <remarks>
        /// When specified as the <tt>shareAccess</tt> constructor parameter,
        /// other SMB clients will be permitted to delete the target file while
        /// this file is open. This constant may be logically OR'd with other share
        /// access flags.
        /// </remarks>
        public const int FileShareDelete = 0x04;

        /// <summary>
        /// A file with this bit on as returned by <tt>getAttributes()</tt> or set
        /// with <tt>setAttributes()</tt> will be read-only
        /// </summary>
        public const int AttrReadonly = 0x01;

        /// <summary>
        /// A file with this bit on as returned by <tt>getAttributes()</tt> or set
        /// with <tt>setAttributes()</tt> will be hidden
        /// </summary>
        public const int AttrHidden = 0x02;

        /// <summary>
        /// A file with this bit on as returned by <tt>getAttributes()</tt> or set
        /// with <tt>setAttributes()</tt> will be a system file
        /// </summary>
        public const int AttrSystem = 0x04;

        /// <summary>
        /// A file with this bit on as returned by <tt>getAttributes()</tt> is
        /// a volume
        /// </summary>
        public const int AttrVolume = 0x08;

        /// <summary>
        /// A file with this bit on as returned by <tt>getAttributes()</tt> is
        /// a directory
        /// </summary>
        public const int AttrDirectory = 0x10;

        /// <summary>
        /// A file with this bit on as returned by <tt>getAttributes()</tt> or set
        /// with <tt>setAttributes()</tt> is an archived file
        /// </summary>
        public const int AttrArchive = 0x20;

        internal const int AttrCompressed = 0x800;

        internal const int AttrNormal = 0x080;

        internal const int AttrTemporary = 0x100;

        internal const int AttrGetMask = 0x7FFF;

        internal const int AttrSetMask = 0x30A7;

        internal const int DefaultAttrExpirationPeriod = 5000;

        internal static readonly int HashDot = ".".GetHashCode();

        internal static readonly int HashDotDot = "..".GetHashCode();

        //internal static LogStream log = LogStream.GetInstance();
        public LogStream Log
        {
            get { return LogStream.GetInstance(); }
        }

        internal static long AttrExpirationPeriod;

        internal static bool IgnoreCopyToException;

        static SmbFile()
        {
            // Open Function Encoding
            // create if the file does not exist
            // fail if the file exists
            // truncate if the file exists
            // share access
            // file attribute encoding
            // extended file attribute encoding(others same as above)
            /*try
            {
                Sharpen.Runtime.GetType("jcifs.Config");
            }
            catch (TypeLoadException cnfe)
            {
                Sharpen.Runtime.PrintStackTrace(cnfe);
            }*/

            AttrExpirationPeriod = Config.GetLong("jcifs.smb.client.attrExpirationPeriod", DefaultAttrExpirationPeriod
                );
            IgnoreCopyToException = Config.GetBoolean("jcifs.smb.client.ignoreCopyToException"
                , true);
            Dfs = new Dfs();
        }

        /// <summary>
        /// Returned by
        /// <see cref="GetType()">GetType()</see>
        /// if the resource this <tt>SmbFile</tt>
        /// represents is a regular file or directory.
        /// </summary>
        public const int TypeFilesystem = 0x01;

        /// <summary>
        /// Returned by
        /// <see cref="GetType()">GetType()</see>
        /// if the resource this <tt>SmbFile</tt>
        /// represents is a workgroup.
        /// </summary>
        public const int TypeWorkgroup = 0x02;

        /// <summary>
        /// Returned by
        /// <see cref="GetType()">GetType()</see>
        /// if the resource this <tt>SmbFile</tt>
        /// represents is a server.
        /// </summary>
        public const int TypeServer = 0x04;

        /// <summary>
        /// Returned by
        /// <see cref="GetType()">GetType()</see>
        /// if the resource this <tt>SmbFile</tt>
        /// represents is a share.
        /// </summary>
        public const int TypeShare = 0x08;

        /// <summary>
        /// Returned by
        /// <see cref="GetType()">GetType()</see>
        /// if the resource this <tt>SmbFile</tt>
        /// represents is a named pipe.
        /// </summary>
        public const int TypeNamedPipe = 0x10;

        /// <summary>
        /// Returned by
        /// <see cref="GetType()">GetType()</see>
        /// if the resource this <tt>SmbFile</tt>
        /// represents is a printer.
        /// </summary>
        public const int TypePrinter = 0x20;

        /// <summary>
        /// Returned by
        /// <see cref="GetType()">GetType()</see>
        /// if the resource this <tt>SmbFile</tt>
        /// represents is a communications device.
        /// </summary>
        public const int TypeComm = 0x40;

        private string _canon;

        private string _share;

        private long _createTime;

        private long _lastModified;

        private int _attributes;

        private long _attrExpiration;

        private long _size;

        private long _sizeExpiration;

        private bool _isExists;

        private int _shareAccess = FileShareRead | FileShareWrite | FileShareDelete;

        private bool _enableDfs = Config.GetBoolean("jcifs.smb.client.enabledfs", false);

        private SmbComBlankResponse _blankResp;

        private DfsReferral _dfsReferral;

        protected internal static Dfs Dfs;

        internal NtlmPasswordAuthentication Auth;

        internal SmbTree Tree;

        internal string Unc;

        internal int Fid;

        internal int Type;

        internal bool Opened;

        internal int TreeNum;

        public bool EnableDfs
        {
            get { return _enableDfs; }
            set { _enableDfs = value; }
        }

        /// <summary>
        /// Constructs an SmbFile representing a resource on an SMB network such as
        /// a file or directory.
        /// </summary>
        /// <remarks>
        /// Constructs an SmbFile representing a resource on an SMB network such as
        /// a file or directory. See the description and examples of smb URLs above.
        /// </remarks>
        /// <param name="url">A URL string</param>
        /// <exception cref="System.UriFormatException">
        /// If the <code>parent</code> and <code>child</code> parameters
        /// do not follow the prescribed syntax
        /// </exception>
        public SmbFile(string url)
            : this(new Uri(url))
        {
        }

        /// <summary>
        /// Constructs an SmbFile representing a resource on an SMB network such
        /// as a file or directory.
        /// </summary>
        /// <remarks>
        /// Constructs an SmbFile representing a resource on an SMB network such
        /// as a file or directory. The second parameter is a relative path from
        /// the <code>parent SmbFile</code>. See the description above for examples
        /// of using the second <code>name</code> parameter.
        /// </remarks>
        /// <param name="context">A base <code>SmbFile</code></param>
        /// <param name="name">A path string relative to the <code>parent</code> paremeter</param>
        /// <exception cref="System.UriFormatException">
        /// If the <code>parent</code> and <code>child</code> parameters
        /// do not follow the prescribed syntax
        /// </exception>
        /// <exception cref="UnknownHostException">If the server or workgroup of the <tt>context</tt> file cannot be determined
        /// 	</exception>
        public SmbFile(SmbFile context, string name)
            : this(context.IsWorkgroup0
                () ? new Uri("smb://" + name) : new Uri(context.Url.AbsoluteUri + name),
                context.Auth)
        {

            this._enableDfs = context.EnableDfs;

            if (!context.IsWorkgroup0())
            {
                Addresses = context.Addresses;

                if (context._share != null)
                {
                    Tree = context.Tree;
                    _dfsReferral = context._dfsReferral;
                }                
            }
        }

        /// <summary>
        /// Constructs an SmbFile representing a resource on an SMB network such
        /// as a file or directory.
        /// </summary>
        /// <remarks>
        /// Constructs an SmbFile representing a resource on an SMB network such
        /// as a file or directory. The second parameter is a relative path from
        /// the <code>parent</code>. See the description above for examples of
        /// using the second <code>chile</code> parameter.
        /// </remarks>
        /// <param name="context">A URL string</param>
        /// <param name="name">A path string relative to the <code>context</code> paremeter</param>
        /// <exception cref="System.UriFormatException">
        /// If the <code>context</code> and <code>name</code> parameters
        /// do not follow the prescribed syntax
        /// </exception>
        /*public SmbFile(string context, string name)
            : this(new Uri(new Uri(null, context), name))
        {
        }*/

        public SmbFile(string context, string name)
            : this(new Uri(context + name))
        {

        }


        /// <summary>
        /// Constructs an SmbFile representing a resource on an SMB network such
        /// as a file or directory.
        /// </summary>
        /// <remarks>
        /// Constructs an SmbFile representing a resource on an SMB network such
        /// as a file or directory.
        /// </remarks>
        /// <param name="url">A URL string</param>
        /// <param name="auth">The credentials the client should use for authentication</param>
        /// <exception cref="System.UriFormatException">If the <code>url</code> parameter does not follow the prescribed syntax
        /// 	</exception>
        public SmbFile(string url, NtlmPasswordAuthentication auth)
            : this(new Uri(url, UriKind.RelativeOrAbsolute),
             auth)
        {

        }

        /// <summary>Constructs an SmbFile representing a file on an SMB network.</summary>
        /// <remarks>
        /// Constructs an SmbFile representing a file on an SMB network. The
        /// <tt>shareAccess</tt> parameter controls what permissions other
        /// clients have when trying to access the same file while this instance
        /// is still open. This value is either <tt>FILE_NO_SHARE</tt> or any
        /// combination of <tt>FILE_SHARE_READ</tt>, <tt>FILE_SHARE_WRITE</tt>,
        /// and <tt>FILE_SHARE_DELETE</tt> logically OR'd together.
        /// </remarks>
        /// <param name="url">A URL string</param>
        /// <param name="auth">The credentials the client should use for authentication</param>
        /// <param name="shareAccess">Specifies what access other clients have while this file is open.
        /// 	</param>
        /// <exception cref="System.UriFormatException">If the <code>url</code> parameter does not follow the prescribed syntax
        /// 	</exception>
        public SmbFile(string url, NtlmPasswordAuthentication auth, int shareAccess)
            : this
                (new Uri(url), auth)
        {
            // Initially null; set by getUncPath; dir must end with '/'
            // Can be null
            // For getDfsPath() and getServerWithDfs()
            // Cannot be null
            // Initially null
            // Initially null; set by getUncPath; never ends with '/'
            // Initially 0; set by open()
            if ((shareAccess & ~(FileShareRead | FileShareWrite | FileShareDelete)) !=
                0)
            {
                throw new RuntimeException("Illegal shareAccess parameter");
            }
            this._shareAccess = shareAccess;
        }

        /// <summary>
        /// Constructs an SmbFile representing a resource on an SMB network such
        /// as a file or directory.
        /// </summary>
        /// <remarks>
        /// Constructs an SmbFile representing a resource on an SMB network such
        /// as a file or directory. The second parameter is a relative path from
        /// the <code>context</code>. See the description above for examples of
        /// using the second <code>name</code> parameter.
        /// </remarks>
        /// <param name="context">A URL string</param>
        /// <param name="name">A path string relative to the <code>context</code> paremeter</param>
        /// <param name="auth">The credentials the client should use for authentication</param>
        /// <exception cref="System.UriFormatException">
        /// If the <code>context</code> and <code>name</code> parameters
        /// do not follow the prescribed syntax
        /// </exception>
        public SmbFile(string context, string name, NtlmPasswordAuthentication auth)
            : this
                (new Uri(context + name)
                , auth)
        {

        }


        /// <summary>
        /// Constructs an SmbFile representing a resource on an SMB network such
        /// as a file or directory.
        /// </summary>
        /// <remarks>
        /// Constructs an SmbFile representing a resource on an SMB network such
        /// as a file or directory. The second parameter is a relative path from
        /// the <code>context</code>. See the description above for examples of
        /// using the second <code>name</code> parameter. The <tt>shareAccess</tt>
        /// parameter controls what permissions other clients have when trying
        /// to access the same file while this instance is still open. This
        /// value is either <tt>FILE_NO_SHARE</tt> or any combination
        /// of <tt>FILE_SHARE_READ</tt>, <tt>FILE_SHARE_WRITE</tt>, and
        /// <tt>FILE_SHARE_DELETE</tt> logically OR'd together.
        /// </remarks>
        /// <param name="context">A URL string</param>
        /// <param name="name">A path string relative to the <code>context</code> paremeter</param>
        /// <param name="auth">The credentials the client should use for authentication</param>
        /// <param name="shareAccess">Specifies what access other clients have while this file is open.
        /// 	</param>
        /// <exception cref="System.UriFormatException">
        /// If the <code>context</code> and <code>name</code> parameters
        /// do not follow the prescribed syntax
        /// </exception>
        public SmbFile(string context, string name, NtlmPasswordAuthentication auth, int
            shareAccess)
            : this(new Uri(context + name), auth)
        {
            if ((shareAccess & ~(FileShareRead | FileShareWrite | FileShareDelete)) !=
                0)
            {
                throw new RuntimeException("Illegal shareAccess parameter");
            }
            this._shareAccess = shareAccess;
        }

        /// <summary>
        /// Constructs an SmbFile representing a resource on an SMB network such
        /// as a file or directory.
        /// </summary>
        /// <remarks>
        /// Constructs an SmbFile representing a resource on an SMB network such
        /// as a file or directory. The second parameter is a relative path from
        /// the <code>context</code>. See the description above for examples of
        /// using the second <code>name</code> parameter. The <tt>shareAccess</tt>
        /// parameter controls what permissions other clients have when trying
        /// to access the same file while this instance is still open. This
        /// value is either <tt>FILE_NO_SHARE</tt> or any combination
        /// of <tt>FILE_SHARE_READ</tt>, <tt>FILE_SHARE_WRITE</tt>, and
        /// <tt>FILE_SHARE_DELETE</tt> logically OR'd together.
        /// </remarks>
        /// <param name="context">A base <code>SmbFile</code></param>
        /// <param name="name">A path string relative to the <code>context</code> file path</param>
        /// <param name="shareAccess">Specifies what access other clients have while this file is open.
        /// 	</param>
        /// <exception cref="System.UriFormatException">
        /// If the <code>context</code> and <code>name</code> parameters
        /// do not follow the prescribed syntax
        /// </exception>
        /// <exception cref="UnknownHostException"></exception>
        public SmbFile(SmbFile context, string name, int shareAccess)
            : this(context.IsWorkgroup0() ? new Uri("smb://" + name) : new Uri(
                          context.Url.AbsoluteUri + name), context.Auth)
        {
            if ((shareAccess & ~(FileShareRead | FileShareWrite | FileShareDelete)) !=
                0)
            {
                throw new RuntimeException("Illegal shareAccess parameter");
            }

            if (!context.IsWorkgroup0())
            {
                this.Addresses = context.Addresses;

                if (context._share != null || context.Tree != null)
                {
                    Tree = context.Tree;
                    _dfsReferral = context._dfsReferral;
                }
            }

            this._shareAccess = shareAccess;
            this._enableDfs = context.EnableDfs;
        }

        /// <summary>
        /// Constructs an SmbFile representing a resource on an SMB network such
        /// as a file or directory from a <tt>URL</tt> object.
        /// </summary>
        /// <remarks>
        /// Constructs an SmbFile representing a resource on an SMB network such
        /// as a file or directory from a <tt>URL</tt> object.
        /// </remarks>
        /// <param name="url">The URL of the target resource</param>
        protected SmbFile(Uri url)
            : this(url, new NtlmPasswordAuthentication(url.GetUserInfo
                ()))
        {
        }

        /// <summary>
        /// Constructs an SmbFile representing a resource on an SMB network such
        /// as a file or directory from a <tt>URL</tt> object and an
        /// <tt>NtlmPasswordAuthentication</tt> object.
        /// </summary>
        /// <remarks>
        /// Constructs an SmbFile representing a resource on an SMB network such
        /// as a file or directory from a <tt>URL</tt> object and an
        /// <tt>NtlmPasswordAuthentication</tt> object.
        /// </remarks>
        /// <param name="url">The URL of the target resource</param>
        /// <param name="auth">The credentials the client should use for authentication</param>
        public SmbFile(Uri url, NtlmPasswordAuthentication auth)
        {
            this.Auth = auth ?? new NtlmPasswordAuthentication(url.GetUserInfo());
            Url = url;
            GetUncPath0();
        }

        /// <exception cref="System.UriFormatException"></exception>
        /// <exception cref="UnknownHostException"></exception>
        /*internal SmbFile(Jcifs.Smb.SmbFile context, string name, int type, int attributes
            , long createTime, long lastModified, long size)
            : this(context.IsWorkgroup0() ?
                new Uri(null, "smb://" + name + "/") : new Uri(context.url,
                name + ((attributes & ATTR_DIRECTORY) > 0 ? "/" : string.Empty)))*/
        internal SmbFile(SmbFile context, string name, int type, int attributes
            , long createTime, long lastModified, long size)
            : this(context.IsWorkgroup0() ?
                new Uri("smb://" + name + "/") : new Uri(context.Url.AbsoluteUri +
                name + ((attributes & AttrDirectory) > 0 ? "/" : string.Empty)))
        {
            Auth = context.Auth;
            if (context._share != null)
            {
                Tree = context.Tree;
                _dfsReferral = context._dfsReferral;
            }
            int last = name.Length - 1;
            if (name[last] == '/')
            {
                name = Runtime.Substring(name, 0, last);
            }
            if (context._share == null)
            {
                Unc = "\\";
            }
            else
            {
                if (context.Unc.Equals("\\"))
                {
                    Unc = '\\' + name;
                }
                else
                {
                    Unc = context.Unc + '\\' + name;
                }
            }

            if (!context.IsWorkgroup0())
            {
                Addresses = context.Addresses;
            }

            this._enableDfs = context.EnableDfs;

            this.Type = type;
            this._attributes = attributes;
            this._createTime = createTime;
            this._lastModified = lastModified;
            this._size = size;
            _isExists = true;
            _attrExpiration = _sizeExpiration = Runtime.CurrentTimeMillis() + AttrExpirationPeriod;
        }

        private SmbComBlankResponse Blank_resp()
        {
            if (_blankResp == null)
            {
                _blankResp = new SmbComBlankResponse();
            }
            return _blankResp;
        }

        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        internal virtual void ResolveDfs(ServerMessageBlock request)
        {
            if (!_enableDfs)
            {
                Connect0();
                return;
            }

            if (request is SmbComClose)
            {
                return;
            }
            Connect0();
            DfsReferral dr = Dfs.Resolve(Tree.Session.transport.TconHostName, Tree.Share, Unc
                , Auth);
            if (dr != null)
            {
                string service = null;
                if (request != null)
                {
                    switch (request.Command)
                    {
                        case ServerMessageBlock.SmbComTransaction:
                        case ServerMessageBlock.SmbComTransaction2:
                            {
                                switch (((SmbComTransaction)request).SubCommand & 0xFF)
                                {
                                    case SmbComTransaction.Trans2GetDfsReferral:
                                        {
                                            break;
                                        }

                                    default:
                                        {
                                            service = "A:";
                                            break;
                                        }
                                }
                                break;
                            }

                        default:
                            {
                                service = "A:";
                                break;
                            }
                    }
                }
                DfsReferral start = dr;
                SmbException se = null;
                do
                {
                    try
                    {
                        if (Log.Level >= 2)
                        {
                            Log.WriteLine("DFS redirect: " + dr);
                        }
                        UniAddress addr = UniAddress.GetByName(dr.Server);
                        SmbTransport trans = SmbTransport.GetSmbTransport(addr, Url.Port);
                        trans.Connect();
                        Tree = trans.GetSmbSession(Auth).GetSmbTree(dr.Share, service);
                        if (dr != start && dr.Key != null)
                        {
                            dr.Map.Put(dr.Key, dr);
                        }
                        se = null;
                        break;
                    }
                    catch (IOException ioe)
                    {
                        if (ioe is SmbException)
                        {
                            se = (SmbException)ioe;
                        }
                        else
                        {
                            se = new SmbException(dr.Server, ioe);
                        }
                    }
                    dr = dr.Next;
                }
                while (dr != start);
                if (se != null)
                {
                    throw se;
                }
                if (Log.Level >= 3)
                {
                    Log.WriteLine(dr);
                }
                _dfsReferral = dr;
                if (dr.PathConsumed < 0)
                {
                    dr.PathConsumed = 0;
                }
                else
                {
                    if (dr.PathConsumed > Unc.Length)
                    {
                        dr.PathConsumed = Unc.Length;
                    }
                }
                string dunc = Runtime.Substring(Unc, dr.PathConsumed);
                if (dunc.Equals(string.Empty))
                {
                    dunc = "\\";
                }
                if (!dr.Path.Equals(string.Empty))
                {
                    dunc = "\\" + dr.Path + dunc;
                }
                Unc = dunc;
                if (request != null && request.Path != null && request.Path.EndsWith("\\") && dunc
                    .EndsWith("\\") == false)
                {
                    dunc += "\\";
                }
                if (request != null)
                {
                    request.Path = dunc;
                    request.Flags2 |= SmbConstants.Flags2ResolvePathsInDfs;
                }
            }
            else
            {
                if (Tree.InDomainDfs && !(request is NtTransQuerySecurityDesc) && !(request is SmbComClose
                    ) && !(request is SmbComFindClose2))
                {
                    throw new SmbException(NtStatus.NtStatusNotFound, false);
                }
                if (request != null)
                {
                    request.Flags2 &= ~SmbConstants.Flags2ResolvePathsInDfs;
                }
            }
        }

        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        internal virtual void Send(ServerMessageBlock request, ServerMessageBlock response
            )
        {
            for (; ; )
            {
                ResolveDfs(request);
                try
                {
                    Tree.Send(request, response);
                    break;
                }
                catch (DfsReferral dre)
                {
                    if (dre.ResolveHashes)
                    {
                        throw;
                    }
                    request.Reset();
                }
            }
        }

        internal static string QueryLookup(string query, string param)
        {
            char[] instr = query.ToCharArray();
            int i;
            int ch;
            int st;
            int eq;
            st = eq = 0;
            for (i = 0; i < instr.Length; i++)
            {
                ch = instr[i];
                if (ch == '&')
                {
                    if (eq > st)
                    {
                        string p = new string(instr, st, eq - st);
                        if (Runtime.EqualsIgnoreCase(p, param))
                        {
                            eq++;
                            return new string(instr, eq, i - eq);
                        }
                    }
                    st = i + 1;
                }
                else
                {
                    if (ch == '=')
                    {
                        eq = i;
                    }
                }
            }
            if (eq > st)
            {
                string p = new string(instr, st, eq - st);
                if (Runtime.EqualsIgnoreCase(p, param))
                {
                    eq++;
                    return new string(instr, eq, instr.Length - eq);
                }
            }
            return null;
        }

        internal UniAddress[] Addresses;

        internal int AddressIndex;

        /// <exception cref="UnknownHostException"></exception>
        internal virtual UniAddress GetAddress()
        {
            if (AddressIndex == 0)
            {
                return GetFirstAddress();
            }
            return Addresses[AddressIndex - 1];
        }

        /// <exception cref="UnknownHostException"></exception>
        internal virtual UniAddress GetFirstAddress()
        {
            AddressIndex = 0;
            string host = Url.GetHost();
            string path = Url.AbsolutePath;
            string query = Url.GetQuery();

            if (Addresses != null && Addresses.Length > 0)
            {
                return GetNextAddress();
            }

            if (query != null)
            {
                string server = QueryLookup(query, "server");
                if (!string.IsNullOrEmpty(server))
                {
                    Addresses = new UniAddress[1];
                    Addresses[0] = UniAddress.GetByName(server);
                    return GetNextAddress();
                }
                string address = QueryLookup(query, "address");
                if (!string.IsNullOrEmpty(address))
                {
                    byte[] ip = Extensions.GetAddressByName(address).GetAddressBytes();
                    Addresses = new UniAddress[1];
                    //addresses[0] = new UniAddress(IPAddress.Parse(host, ip));
                    Addresses[0] = new UniAddress(IPAddress.Parse(host));
                    return GetNextAddress();
                }
            }
            if (host.Length == 0)
            {
                try
                {
                    NbtAddress addr = NbtAddress.GetByName(NbtAddress.MasterBrowserName, 0x01, null);
                    Addresses = new UniAddress[1];
                    Addresses[0] = UniAddress.GetByName(addr.GetHostAddress());
                }
                catch (UnknownHostException uhe)
                {
                    NtlmPasswordAuthentication.InitDefaults();
                    if (NtlmPasswordAuthentication.DefaultDomain.Equals("?"))
                    {
                        throw;
                    }
                    Addresses = UniAddress.GetAllByName(NtlmPasswordAuthentication.DefaultDomain, true
                        );
                }
            }
            else
            {
                if (path.Length == 0 || path.Equals("/"))
                {
                    Addresses = UniAddress.GetAllByName(host, true);
                }
                else
                {
                    Addresses = UniAddress.GetAllByName(host, false);
                }
            }
            return GetNextAddress();
        }

        internal virtual UniAddress GetNextAddress()
        {
            UniAddress addr = null;
            if (AddressIndex < Addresses.Length)
            {
                addr = Addresses[AddressIndex++];
            }
            return addr;
        }

        internal virtual bool HasNextAddress()
        {
            return AddressIndex < Addresses.Length;
        }

        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        internal virtual void Connect0()
        {
            try
            {
                Connect();
            }
            catch (UnknownHostException uhe)
            {
                throw new SmbException("Failed to connect to server", uhe);
            }
            catch (SmbException se)
            {
                throw;
            }
            catch (IOException ioe)
            {
                throw new SmbException("Failed to connect to server", ioe);
            }
        }

        /// <exception cref="System.IO.IOException"></exception>
        internal virtual void DoConnect()
        {
            SmbTransport trans;
            UniAddress addr;
            addr = GetAddress();

            if (Tree != null && Tree.Session.transport.Address.Equals(addr))
            {
                trans = Tree.Session.transport;
            }
            else
            {
                trans = SmbTransport.GetSmbTransport(addr, Url.Port);
                Tree = trans.GetSmbSession(Auth).GetSmbTree(_share, null);
            }


            string hostName = GetServerWithDfs();
            if (_enableDfs)
            {
                Tree.InDomainDfs = Dfs.Resolve(hostName, Tree.Share, null, Auth) != null;
            }
            if (Tree.InDomainDfs)
            {
                Tree.ConnectionState = 2;
            }
            try
            {
                if (Log.Level >= 3)
                {
                    Log.WriteLine("doConnect: " + addr);
                }
                Tree.TreeConnect(null, null);
            }
            catch (SmbAuthException sae)
            {
                NtlmPasswordAuthentication a;
                SmbSession ssn;
                if (_share == null)
                {
                    // IPC$ - try "anonymous" credentials
                    ssn = trans.GetSmbSession(NtlmPasswordAuthentication.Null);
                    Tree = ssn.GetSmbTree(null, null);
                    Tree.TreeConnect(null, null);
                }
                else
                {
                    if ((a = NtlmAuthenticator.RequestNtlmPasswordAuthentication(Url.ToString(), sae)
                        ) != null)
                    {
                        Auth = a;
                        ssn = trans.GetSmbSession(Auth);
                        Tree = ssn.GetSmbTree(_share, null);
                        Tree.InDomainDfs = Dfs.Resolve(hostName, Tree.Share, null, Auth) != null;
                        if (Tree.InDomainDfs)
                        {
                            Tree.ConnectionState = 2;
                        }
                        Tree.TreeConnect(null, null);
                    }
                    else
                    {
                        if (Log.Level >= 1 && HasNextAddress())
                        {
                            Runtime.PrintStackTrace(sae, Log);
                        }
                        throw;
                    }
                }
            }
        }

        /// <summary>It is not necessary to call this method directly.</summary>
        /// <remarks>
        /// It is not necessary to call this method directly. This is the
        /// <tt>URLConnection</tt> implementation of <tt>connect()</tt>.
        /// </remarks>
        /// <exception cref="System.IO.IOException"></exception>
        public void Connect()
        {
            SmbTransport trans;
            SmbSession ssn;
            UniAddress addr;
            if (IsConnected())
            {
                return;
            }
            GetUncPath0();
            GetFirstAddress();
            for (; ; )
            {
                try
                {
                    DoConnect();
                    return;
                }
                catch (SmbAuthException sae)
                {
                    throw;
                }
                catch (SmbException se)
                {
                    // Prevents account lockout on servers with multiple IPs
                    if (GetNextAddress() == null)
                    {
                        throw;
                    }
                    else
                    {
                        RemoveCurrentAddress();
                    }

                    if (Log.Level >= 3)
                    {
                        Runtime.PrintStackTrace(se, Log);
                    }
                }
            }
        }

        internal virtual bool IsConnected()
        {
            return Tree != null && Tree.ConnectionState == 2;
        }

        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        internal virtual int Open0(int flags, int access, int attrs, int options)
        {
            int f;
            Connect0();
            if (Log.Level >= 3)
            {
                Log.WriteLine("open0: " + Unc);
            }
            if (Tree.Session.transport.HasCapability(SmbConstants.CapNtSmbs))
            {
                SmbComNtCreateAndXResponse response = new SmbComNtCreateAndXResponse();
                SmbComNtCreateAndX request = new SmbComNtCreateAndX(Unc, flags, access, _shareAccess
                    , attrs, options, null);
                if (this is SmbNamedPipe)
                {
                    request.Flags0 |= 0x16;
                    request.DesiredAccess |= 0x20000;
                    response.IsExtended = true;
                }
                Send(request, response);
                f = response.Fid;
                _attributes = response.ExtFileAttributes & AttrGetMask;
                _attrExpiration = Runtime.CurrentTimeMillis() + AttrExpirationPeriod;
                _isExists = true;
            }
            else
            {
                SmbComOpenAndXResponse response = new SmbComOpenAndXResponse();
                Send(new SmbComOpenAndX(Unc, access, flags, null), response);
                f = response.Fid;
            }
            return f;
        }

        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        internal virtual void Open(int flags, int access, int attrs, int options)
        {
            if (IsOpen())
            {
                return;
            }
            Fid = Open0(flags, access, attrs, options);
            Opened = true;
            TreeNum = Tree.TreeNum;
        }

        internal virtual bool IsOpen()
        {
            bool ans = Opened && IsConnected() && TreeNum == Tree.TreeNum;
            return ans;
        }

        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        internal virtual void Close(int f, long lastWriteTime)
        {
            if (Log.Level >= 3)
            {
                Log.WriteLine("close: " + f);
            }
            Send(new SmbComClose(f, lastWriteTime), Blank_resp());
        }

        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        internal virtual void Close(long lastWriteTime)
        {
            if (IsOpen() == false)
            {
                return;
            }
            Close(Fid, lastWriteTime);
            Opened = false;
        }

        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        internal virtual void Close()
        {
            Close(0L);
        }

        /// <summary>
        /// Returns the <tt>NtlmPasswordAuthentication</tt> object used as
        /// credentials with this file or pipe.
        /// </summary>
        /// <remarks>
        /// Returns the <tt>NtlmPasswordAuthentication</tt> object used as
        /// credentials with this file or pipe. This can be used to retrieve the
        /// username for example:
        /// <tt>
        /// String username = f.getPrincipal().getName();
        /// </tt>
        /// The <tt>Principal</tt> object returned will never be <tt>null</tt>
        /// however the username can be <tt>null</tt> indication anonymous
        /// credentials were used (e.g. some IPC$ services).
        /// </remarks>
        public virtual Principal GetPrincipal()
        {
            return Auth;
        }

        /// <summary>Returns the last component of the target URL.</summary>
        /// <remarks>
        /// Returns the last component of the target URL. This will
        /// effectively be the name of the file or directory represented by this
        /// <code>SmbFile</code> or in the case of URLs that only specify a server
        /// or workgroup, the server or workgroup will be returned. The name of
        /// the root URL <code>smb://</code> is also <code>smb://</code>. If this
        /// <tt>SmbFile</tt> refers to a workgroup, server, share, or directory,
        /// the name will include a trailing slash '/' so that composing new
        /// <tt>SmbFile</tt>s will maintain the trailing slash requirement.
        /// </remarks>
        /// <returns>
        /// The last component of the URL associated with this SMB
        /// resource or <code>smb://</code> if the resource is <code>smb://</code>
        /// itself.
        /// </returns>
        public virtual string GetName()
        {
            GetUncPath0();
            if (_canon.Length > 1)
            {
                int i = _canon.Length - 2;
                while (_canon[i] != '/')
                {
                    i--;
                }
                return Runtime.Substring(_canon, i + 1);
            }
            if (_share != null)
            {
                return _share + '/';
            }
            if (Url.GetHost().Length > 0)
            {
                return Url.GetHost() + '/';
            }
            return "smb://";
        }

        /// <summary>
        /// Everything but the last component of the URL representing this SMB
        /// resource is effectivly it's parent.
        /// </summary>
        /// <remarks>
        /// Everything but the last component of the URL representing this SMB
        /// resource is effectivly it's parent. The root URL <code>smb://</code>
        /// does not have a parent. In this case <code>smb://</code> is returned.
        /// </remarks>
        /// <returns>
        /// The parent directory of this SMB resource or
        /// <code>smb://</code> if the resource refers to the root of the URL
        /// hierarchy which incedentally is also <code>smb://</code>.
        /// </returns>
        public virtual string GetParent()
        {
            string str = Url.Authority;
            if (str.Length > 0)
            {
                StringBuilder sb = new StringBuilder("smb://");
                sb.Append(str);
                GetUncPath0();
                if (_canon.Length > 1)
                {
                    sb.Append(_canon);
                }
                else
                {
                    sb.Append('/');
                }
                str = sb.ToString();
                int i = str.Length - 2;
                while (str[i] != '/')
                {
                    i--;
                }
                return Runtime.Substring(str, 0, i + 1);
            }
            return "smb://";
        }

        /// <summary>Returns the full uncanonicalized URL of this SMB resource.</summary>
        /// <remarks>
        /// Returns the full uncanonicalized URL of this SMB resource. An
        /// <code>SmbFile</code> constructed with the result of this method will
        /// result in an <code>SmbFile</code> that is equal to the original.
        /// </remarks>
        /// <returns>The uncanonicalized full URL of this SMB resource.</returns>
        public virtual string GetPath()
        {
            return Url.ToString();
        }

        internal virtual string GetUncPath0()
        {
            if (Unc == null)
            {
                char[] instr = Url.LocalPath.ToCharArray();
                char[] outstr = new char[instr.Length];
                int length = instr.Length;
                int i;
                int o;
                int state;

                state = 0;
                o = 0;
                for (i = 0; i < length; i++)
                {
                    switch (state)
                    {
                        case 0:
                            {
                                if (instr[i] != '/')
                                {
                                    return null;
                                }
                                outstr[o++] = instr[i];
                                state = 1;
                                break;
                            }

                        case 1:
                            {
                                if (instr[i] == '/')
                                {
                                    break;
                                }
                                if (instr[i] == '.' && ((i + 1) >= length || instr[i + 1] == '/'))
                                {
                                    i++;
                                    break;
                                }
                                if ((i + 1) < length && instr[i] == '.' && instr[i + 1] == '.' && ((i + 2) >= length
                                                                                               || instr[i + 2] == '/'))
                                {
                                    i += 2;
                                    if (o == 1)
                                    {
                                        break;
                                    }
                                    do
                                    {
                                        o--;
                                    }
                                    while (o > 1 && outstr[o - 1] != '/');
                                    break;
                                }
                                state = 2;
                                goto case 2;
                            }

                        case 2:
                            {
                                if (instr[i] == '/')
                                {
                                    state = 1;
                                }
                                outstr[o++] = instr[i];
                                break;
                            }
                    }
                }
                _canon = new string(outstr, 0, o);
                if (o > 1)
                {
                    o--;
                    i = _canon.IndexOf('/', 1);
                    if (i < 0)
                    {
                        _share = Runtime.Substring(_canon, 1);
                        Unc = "\\";
                    }
                    else
                    {
                        if (i == o)
                        {
                            _share = Runtime.Substring(_canon, 1, i);
                            Unc = "\\";
                        }
                        else
                        {
                            _share = Runtime.Substring(_canon, 1, i);
                            Unc = Runtime.Substring(_canon, i, outstr[o] == '/' ? o : o + 1);
                            Unc = Unc.Replace('/', '\\');
                        }
                    }
                }
                else
                {
                    _share = null;
                    Unc = "\\";
                }
            }
            return Unc;
        }

        /// <summary>Retuns the Windows UNC style path with backslashs intead of forward slashes.
        /// 	</summary>
        /// <remarks>Retuns the Windows UNC style path with backslashs intead of forward slashes.
        /// 	</remarks>
        /// <returns>The UNC path.</returns>
        public virtual string GetUncPath()
        {
            GetUncPath0();
            if (_share == null)
            {
                return "\\\\" + Url.GetHost();
            }
            return "\\\\" + Url.GetHost() + _canon.Replace('/', '\\');
        }

        /// <summary>
        /// Returns the full URL of this SMB resource with '.' and '..' components
        /// factored out.
        /// </summary>
        /// <remarks>
        /// Returns the full URL of this SMB resource with '.' and '..' components
        /// factored out. An <code>SmbFile</code> constructed with the result of
        /// this method will result in an <code>SmbFile</code> that is equal to
        /// the original.
        /// </remarks>
        /// <returns>The canonicalized URL of this SMB resource.</returns>
        public virtual string GetCanonicalPath()
        {
            string str = Url.Authority;
            GetUncPath0();
            if (str.Length > 0)
            {
                return "smb://" + Url.Authority + _canon;
            }
            return "smb://";
        }

        /// <summary>Retrieves the share associated with this SMB resource.</summary>
        /// <remarks>
        /// Retrieves the share associated with this SMB resource. In
        /// the case of <code>smb://</code>, <code>smb://workgroup/</code>,
        /// and <code>smb://server/</code> URLs which do not specify a share,
        /// <code>null</code> will be returned.
        /// </remarks>
        /// <returns>The share component or <code>null</code> if there is no share</returns>
        public virtual string GetShare()
        {
            return _share;
        }

        internal virtual string GetServerWithDfs()
        {
            if (_dfsReferral != null)
            {
                return _dfsReferral.Server;
            }
            return GetServer();
        }

        /// <summary>Retrieve the hostname of the server for this SMB resource.</summary>
        /// <remarks>
        /// Retrieve the hostname of the server for this SMB resource. If this
        /// <code>SmbFile</code> references a workgroup, the name of the workgroup
        /// is returned. If this <code>SmbFile</code> refers to the root of this
        /// SMB network hierarchy, <code>null</code> is returned.
        /// </remarks>
        /// <returns>
        /// The server or workgroup name or <code>null</code> if this
        /// <code>SmbFile</code> refers to the root <code>smb://</code> resource.
        /// </returns>
        public virtual string GetServer()
        {
            string str = Url.GetHost();
            if (str.Length == 0)
            {
                return null;
            }
            return str;
        }

        /// <summary>Returns type of of object this <tt>SmbFile</tt> represents.</summary>
        /// <remarks>Returns type of of object this <tt>SmbFile</tt> represents.</remarks>
        /// <returns>
        /// <tt>TYPE_FILESYSTEM, TYPE_WORKGROUP, TYPE_SERVER, TYPE_SHARE,
        /// TYPE_PRINTER, TYPE_NAMED_PIPE</tt>, or <tt>TYPE_COMM</tt>.
        /// </returns>
        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        public new virtual int GetType()
        {
            if (Type == 0)
            {
                if (GetUncPath0().Length > 1)
                {
                    Type = TypeFilesystem;
                }
                else
                {
                    if (_share != null)
                    {
                        // treeConnect good enough to test service type
                        Connect0();
                        if (_share.Equals("IPC$"))
                        {
                            Type = TypeNamedPipe;
                        }
                        else
                        {
                            if (Tree.Service.Equals("LPT1:"))
                            {
                                Type = TypePrinter;
                            }
                            else
                            {
                                if (Tree.Service.Equals("COMM"))
                                {
                                    Type = TypeComm;
                                }
                                else
                                {
                                    Type = TypeShare;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(Url.Authority))
                        {
                            Type = TypeWorkgroup;
                        }
                        else
                        {
                            UniAddress addr;
                            try
                            {
                                addr = GetAddress();
                            }
                            catch (UnknownHostException uhe)
                            {
                                throw new SmbException(Url.ToString(), uhe);
                            }
                            if (addr.GetAddress() is NbtAddress)
                            {
                                int code = ((NbtAddress)addr.GetAddress()).GetNameType();
                                if (code == 0x1d || code == 0x1b)
                                {
                                    Type = TypeWorkgroup;
                                    return Type;
                                }
                            }
                            Type = TypeServer;
                        }
                    }
                }
            }
            return Type;
        }

        /// <exception cref="UnknownHostException"></exception>
        internal virtual bool IsWorkgroup0()
        {
            if (Type == TypeWorkgroup || Url.GetHost().Length == 0)
            {
                Type = TypeWorkgroup;
                return true;
            }
            GetUncPath0();
            if (_share == null)
            {
                UniAddress addr = GetAddress();
                if (addr.GetAddress() is NbtAddress)
                {
                    int code = ((NbtAddress)addr.GetAddress()).GetNameType();
                    if (code == 0x1d || code == 0x1b)
                    {
                        Type = TypeWorkgroup;
                        return true;
                    }
                }
                Type = TypeServer;
            }
            return false;
        }

        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        internal virtual IInfo QueryPath(string path, int infoLevel)
        {
            Connect0();
            if (Log.Level >= 3)
            {
                Log.WriteLine("queryPath: " + path);
            }
            if (Tree.Session.transport.HasCapability(SmbConstants.CapNtSmbs))
            {
                Trans2QueryPathInformationResponse response = new Trans2QueryPathInformationResponse
                    (infoLevel);
                Send(new Trans2QueryPathInformation(path, infoLevel), response);
                return response.Info;
            }
            else
            {
                SmbComQueryInformationResponse response = new SmbComQueryInformationResponse(Tree
                    .Session.transport.Server.ServerTimeZone * 1000 * 60L);
                Send(new SmbComQueryInformation(path), response);
                return response;
            }
        }

        /// <summary>Tests to see if the SMB resource exists.</summary>
        /// <remarks>
        /// Tests to see if the SMB resource exists. If the resource refers
        /// only to a server, this method determines if the server exists on the
        /// network and is advertising SMB services. If this resource refers to
        /// a workgroup, this method determines if the workgroup name is valid on
        /// the local SMB network. If this <code>SmbFile</code> refers to the root
        /// <code>smb://</code> resource <code>true</code> is always returned. If
        /// this <code>SmbFile</code> is a traditional file or directory, it will
        /// be queried for on the specified server as expected.
        /// </remarks>
        /// <returns>
        /// <code>true</code> if the resource exists or is alive or
        /// <code>false</code> otherwise
        /// </returns>
        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        public virtual bool Exists()
        {
            if (_attrExpiration > Runtime.CurrentTimeMillis())
            {
                return _isExists;
            }
            _attributes = AttrReadonly | AttrDirectory;
            _createTime = 0L;
            _lastModified = 0L;
            _isExists = false;
            try
            {
                if (Url.GetHost().Length == 0)
                {
                }
                else
                {
                    if (_share == null)
                    {
                        if (GetType() == TypeWorkgroup)
                        {
                            UniAddress.GetByName(Url.GetHost(), true);
                        }
                        else
                        {
                            UniAddress.GetByName(Url.GetHost()).GetHostName();
                        }
                    }
                    else
                    {
                        if (GetUncPath0().Length == 1 || Runtime.EqualsIgnoreCase(_share, "IPC$"))
                        {
                            Connect0();
                        }
                        else
                        {
                            // treeConnect is good enough
                            IInfo info = QueryPath(GetUncPath0(), Trans2QueryPathInformationResponse.SMB_QUERY_FILE_BASIC_INFO
                                );
                            _attributes = info.GetAttributes();
                            _createTime = info.GetCreateTime();
                            _lastModified = info.GetLastWriteTime();
                        }
                    }
                }
                _isExists = true;
            }
            catch (UnknownHostException)
            {
            }
            catch (SmbException se)
            {
                switch (se.GetNtStatus())
                {
                    case NtStatus.NtStatusNoSuchFile:
                    case NtStatus.NtStatusObjectNameInvalid:
                    case NtStatus.NtStatusObjectNameNotFound:
                    case NtStatus.NtStatusObjectPathNotFound:
                        {
                            break;
                        }

                    default:
                        {
                            throw;
                        }
                }
            }
            _attrExpiration = Runtime.CurrentTimeMillis() + AttrExpirationPeriod;
            return _isExists;
        }

        /// <summary>
        /// Tests to see if the file this <code>SmbFile</code> represents can be
        /// read.
        /// </summary>
        /// <remarks>
        /// Tests to see if the file this <code>SmbFile</code> represents can be
        /// read. Because any file, directory, or other resource can be read if it
        /// exists, this method simply calls the <code>exists</code> method.
        /// </remarks>
        /// <returns><code>true</code> if the file is read-only</returns>
        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        public virtual bool CanRead()
        {
            if (GetType() == TypeNamedPipe)
            {
                // try opening the pipe for reading?
                return true;
            }
            return Exists();
        }

        // try opening and catch sharing violation?
        /// <summary>
        /// Tests to see if the file this <code>SmbFile</code> represents
        /// exists and is not marked read-only.
        /// </summary>
        /// <remarks>
        /// Tests to see if the file this <code>SmbFile</code> represents
        /// exists and is not marked read-only. By default, resources are
        /// considered to be read-only and therefore for <code>smb://</code>,
        /// <code>smb://workgroup/</code>, and <code>smb://server/</code> resources
        /// will be read-only.
        /// </remarks>
        /// <returns>
        /// <code>true</code> if the resource exists is not marked
        /// read-only
        /// </returns>
        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        public virtual bool CanWrite()
        {
            if (GetType() == TypeNamedPipe)
            {
                // try opening the pipe for writing?
                return true;
            }
            return Exists() && (_attributes & AttrReadonly) == 0;
        }

        /// <summary>Tests to see if the file this <code>SmbFile</code> represents is a directory.
        /// 	</summary>
        /// <remarks>Tests to see if the file this <code>SmbFile</code> represents is a directory.
        /// 	</remarks>
        /// <returns><code>true</code> if this <code>SmbFile</code> is a directory</returns>
        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        public virtual bool IsDirectory()
        {
            if (GetUncPath0().Length == 1)
            {
                return true;
            }
            if (!Exists())
            {
                return false;
            }
            return (_attributes & AttrDirectory) == AttrDirectory;
        }

        /// <summary>Tests to see if the file this <code>SmbFile</code> represents is not a directory.
        /// 	</summary>
        /// <remarks>Tests to see if the file this <code>SmbFile</code> represents is not a directory.
        /// 	</remarks>
        /// <returns><code>true</code> if this <code>SmbFile</code> is not a directory</returns>
        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        public virtual bool IsFile()
        {
            if (GetUncPath0().Length == 1)
            {
                return false;
            }
            Exists();
            return (_attributes & AttrDirectory) == 0;
        }

        /// <summary>
        /// Tests to see if the file this SmbFile represents is marked as
        /// hidden.
        /// </summary>
        /// <remarks>
        /// Tests to see if the file this SmbFile represents is marked as
        /// hidden. This method will also return true for shares with names that
        /// end with '$' such as <code>IPC$</code> or <code>C$</code>.
        /// </remarks>
        /// <returns><code>true</code> if the <code>SmbFile</code> is marked as being hidden</returns>
        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        public virtual bool IsHidden()
        {
            if (_share == null)
            {
                return false;
            }
            if (GetUncPath0().Length == 1)
            {
                if (_share.EndsWith("$"))
                {
                    return true;
                }
                return false;
            }
            Exists();
            return (_attributes & AttrHidden) == AttrHidden;
        }

        /// <summary>
        /// If the path of this <code>SmbFile</code> falls within a DFS volume,
        /// this method will return the referral path to which it maps.
        /// </summary>
        /// <remarks>
        /// If the path of this <code>SmbFile</code> falls within a DFS volume,
        /// this method will return the referral path to which it maps. Otherwise
        /// <code>null</code> is returned.
        /// </remarks>
        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        public virtual string GetDfsPath()
        {
            ResolveDfs(null);
            if (_dfsReferral == null)
            {
                return null;
            }
            string path = "smb:/" + _dfsReferral.Server + "/" + _dfsReferral.Share + Unc;
            path = path.Replace('\\', '/');
            if (IsDirectory())
            {
                path += '/';
            }
            return path;
        }

        /// <summary>Retrieve the time this <code>SmbFile</code> was created.</summary>
        /// <remarks>
        /// Retrieve the time this <code>SmbFile</code> was created. The value
        /// returned is suitable for constructing a
        /// <see cref="System.DateTime">System.DateTime</see>
        /// object
        /// (i.e. seconds since Epoch 1970). Times should be the same as those
        /// reported using the properties dialog of the Windows Explorer program.
        /// For Win95/98/Me this is actually the last write time. It is currently
        /// not possible to retrieve the create time from files on these systems.
        /// </remarks>
        /// <returns>
        /// The number of milliseconds since the 00:00:00 GMT, January 1,
        /// 1970 as a <code>long</code> value
        /// </returns>
        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        public virtual long CreateTime()
        {
            if (GetUncPath0().Length > 1)
            {
                Exists();
                return _createTime;
            }
            return 0L;
        }

        /// <summary>
        /// Retrieve the last time the file represented by this
        /// <code>SmbFile</code> was modified.
        /// </summary>
        /// <remarks>
        /// Retrieve the last time the file represented by this
        /// <code>SmbFile</code> was modified. The value returned is suitable for
        /// constructing a
        /// <see cref="System.DateTime">System.DateTime</see>
        /// object (i.e. seconds since Epoch
        /// 1970). Times should be the same as those reported using the properties
        /// dialog of the Windows Explorer program.
        /// </remarks>
        /// <returns>
        /// The number of milliseconds since the 00:00:00 GMT, January 1,
        /// 1970 as a <code>long</code> value
        /// </returns>
        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        public virtual long LastModified()
        {
            if (GetUncPath0().Length > 1)
            {
                Exists();
                return _lastModified;
            }
            return 0L;
        }

        /// <summary>List the contents of this SMB resource.</summary>
        /// <remarks>
        /// List the contents of this SMB resource. The list returned by this
        /// method will be;
        /// <ul>
        /// <li> files and directories contained within this resource if the
        /// resource is a normal disk file directory,
        /// <li> all available NetBIOS workgroups or domains if this resource is
        /// the top level URL <code>smb://</code>,
        /// <li> all servers registered as members of a NetBIOS workgroup if this
        /// resource refers to a workgroup in a <code>smb://workgroup/</code> URL,
        /// <li> all browseable shares of a server including printers, IPC
        /// services, or disk volumes if this resource is a server URL in the form
        /// <code>smb://server/</code>,
        /// <li> or <code>null</code> if the resource cannot be resolved.
        /// </ul>
        /// </remarks>
        /// <returns>
        /// A <code>String[]</code> array of files and directories,
        /// workgroups, servers, or shares depending on the context of the
        /// resource URL
        /// </returns>
        /// <exception cref="SmbException"></exception>
        public virtual string[] List()
        {
            return List("*", AttrDirectory | AttrHidden | AttrSystem, null, null);
        }

        /// <summary>List the contents of this SMB resource.</summary>
        /// <remarks>
        /// List the contents of this SMB resource. The list returned will be
        /// identical to the list returned by the parameterless <code>list()</code>
        /// method minus filenames filtered by the specified filter.
        /// </remarks>
        /// <param name="filter">a filename filter to exclude filenames from the results</param>
        /// <exception cref="SmbException"># @return An array of filenames</exception>
        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        public virtual string[] List(ISmbFilenameFilter filter)
        {
            return List("*", AttrDirectory | AttrHidden | AttrSystem, filter, null);
        }

        /// <summary>
        /// List the contents of this SMB resource as an array of
        /// <code>SmbFile</code> objects.
        /// </summary>
        /// <remarks>
        /// List the contents of this SMB resource as an array of
        /// <code>SmbFile</code> objects. This method is much more efficient than
        /// the regular <code>list</code> method when querying attributes of each
        /// file in the result set.
        /// <p>
        /// The list of <code>SmbFile</code>s returned by this method will be;
        /// <ul>
        /// <li> files and directories contained within this resource if the
        /// resource is a normal disk file directory,
        /// <li> all available NetBIOS workgroups or domains if this resource is
        /// the top level URL <code>smb://</code>,
        /// <li> all servers registered as members of a NetBIOS workgroup if this
        /// resource refers to a workgroup in a <code>smb://workgroup/</code> URL,
        /// <li> all browseable shares of a server including printers, IPC
        /// services, or disk volumes if this resource is a server URL in the form
        /// <code>smb://server/</code>,
        /// <li> or <code>null</code> if the resource cannot be resolved.
        /// </ul>
        /// </remarks>
        /// <returns>
        /// An array of <code>SmbFile</code> objects representing file
        /// and directories, workgroups, servers, or shares depending on the context
        /// of the resource URL
        /// </returns>
        /// <exception cref="SmbException"></exception>
        public virtual SmbFile[] ListFiles()
        {
            return ListFiles("*", AttrDirectory | AttrHidden | AttrSystem, null, null);
        }

        /// <summary>
        /// The CIFS protocol provides for DOS "wildcards" to be used as
        /// a performance enhancement.
        /// </summary>
        /// <remarks>
        /// The CIFS protocol provides for DOS "wildcards" to be used as
        /// a performance enhancement. The client does not have to filter
        /// the names and the server does not have to return all directory
        /// entries.
        /// <p>
        /// The wildcard expression may consist of two special meta
        /// characters in addition to the normal filename characters. The '*'
        /// character matches any number of characters in part of a name. If
        /// the expression begins with one or more '?'s then exactly that
        /// many characters will be matched whereas if it ends with '?'s
        /// it will match that many characters <i>or less</i>.
        /// <p>
        /// Wildcard expressions will not filter workgroup names or server names.
        /// <blockquote><pre>
        /// winnt&gt; ls c?o
        /// clock.avi                  -rw--      82944 Mon Oct 14 1996 1:38 AM
        /// Cookies                    drw--          0 Fri Nov 13 1998 9:42 PM
        /// 2 items in 5ms
        /// </pre></blockquote>
        /// </remarks>
        /// <param name="wildcard">a wildcard expression</param>
        /// <exception cref="SmbException">SmbException</exception>
        /// <returns>
        /// An array of <code>SmbFile</code> objects representing file
        /// and directories, workgroups, servers, or shares depending on the context
        /// of the resource URL
        /// </returns>
        /// <exception cref="SmbException"></exception>
        public virtual SmbFile[] ListFiles(string wildcard)
        {
            return ListFiles(wildcard, AttrDirectory | AttrHidden | AttrSystem, null, null
                );
        }

        /// <summary>List the contents of this SMB resource.</summary>
        /// <remarks>
        /// List the contents of this SMB resource. The list returned will be
        /// identical to the list returned by the parameterless <code>listFiles()</code>
        /// method minus files filtered by the specified filename filter.
        /// </remarks>
        /// <param name="filter">a filter to exclude files from the results</param>
        /// <returns>An array of <tt>SmbFile</tt> objects</returns>
        /// <exception cref="SmbException">SmbException</exception>
        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        public virtual SmbFile[] ListFiles(ISmbFilenameFilter filter)
        {
            return ListFiles("*", AttrDirectory | AttrHidden | AttrSystem, filter, null);
        }

        /// <summary>List the contents of this SMB resource.</summary>
        /// <remarks>
        /// List the contents of this SMB resource. The list returned will be
        /// identical to the list returned by the parameterless <code>listFiles()</code>
        /// method minus filenames filtered by the specified filter.
        /// </remarks>
        /// <param name="filter">a file filter to exclude files from the results</param>
        /// <returns>An array of <tt>SmbFile</tt> objects</returns>
        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        public virtual SmbFile[] ListFiles(ISmbFileFilter filter)
        {
            return ListFiles("*", AttrDirectory | AttrHidden | AttrSystem, null, filter);
        }

        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        internal virtual string[] List(string wildcard, int searchAttributes, ISmbFilenameFilter
             fnf, ISmbFileFilter ff)
        {
            List<object> list = new List<object>();
            DoEnum(list, false, wildcard, searchAttributes, fnf, ff);

            return Collections.ToArray<string>(list); //Collections.ToArray<string>(list);
        }

        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        internal virtual SmbFile[] ListFiles(string wildcard, int searchAttributes
            , ISmbFilenameFilter fnf, ISmbFileFilter ff)
        {
            List<object> list = new List<object>();
            DoEnum(list, true, wildcard, searchAttributes, fnf, ff);

            return Collections.ToArray<SmbFile>(list); //Collections.ToArray<SmbFile>(list);
        }

        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        internal virtual void DoEnum(List<object> list, bool files, string wildcard, int searchAttributes
            , ISmbFilenameFilter fnf, ISmbFileFilter ff)
        {
            if (ff != null && ff is DosFileFilter)
            {
                DosFileFilter dff = (DosFileFilter)ff;
                if (dff.Wildcard != null)
                {
                    wildcard = dff.Wildcard;
                }
                searchAttributes = dff.Attributes;
            }
            try
            {
                int hostlen = Url.GetHost() != null ? Url.GetHost().Length : 0;
                if (hostlen == 0 || GetType() == TypeWorkgroup)
                {
                    DoNetServerEnum(list, files, wildcard, searchAttributes, fnf, ff);
                }
                else
                {
                    if (_share == null)
                    {
                        DoShareEnum(list, files, wildcard, searchAttributes, fnf, ff);
                    }
                    else
                    {
                        DoFindFirstNext(list, files, wildcard, searchAttributes, fnf, ff);
                    }
                }
            }
            catch (UnknownHostException uhe)
            {
                throw new SmbException(Url.ToString(), uhe);
            }
            catch (UriFormatException mue)
            {
                throw new SmbException(Url.ToString(), mue);
            }
        }

        private void RemoveCurrentAddress()
        {
            if (AddressIndex >= 1)
            {
                UniAddress[] aux = new UniAddress[Addresses.Length - 1];

                Array.Copy(Addresses, 1, aux, 0, Addresses.Length - 1);

                Addresses = aux;
                AddressIndex--;
            }
        }

        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        /// <exception cref="UnknownHostException"></exception>
        /// <exception cref="System.UriFormatException"></exception>
        internal virtual void DoShareEnum(List<object> list, bool files, string wildcard, int
             searchAttributes, ISmbFilenameFilter fnf, ISmbFileFilter ff)
        {
            string p = Url.AbsolutePath;
            IOException last = null;
            IFileEntry[] entries;
            UniAddress addr;
            IFileEntry e;
            Hashtable map;
            if (p.LastIndexOf('/') != (p.Length - 1))
            {
                throw new SmbException(Url + " directory must end with '/'");
            }
            if (GetType() != TypeServer)
            {
                throw new SmbException("The requested list operations is invalid: " + Url);
            }
            map = new Hashtable();
            if (_enableDfs && Dfs.IsTrustedDomain(GetServer(), Auth))
            {
                try
                {
                    entries = DoDfsRootEnum();
                    for (int ei = 0; ei < entries.Length; ei++)
                    {
                        e = entries[ei];
                        if (map.ContainsKey(e) == false)
                        {
                            map.Put(e, e);
                        }
                    }
                }
                catch (IOException ioe)
                {
                    if (Log.Level >= 4)
                    {
                        Runtime.PrintStackTrace(ioe, Log);
                    }
                }
            }
            addr = GetFirstAddress();
            while (addr != null)
            {
                try
                {
                    last = null;

                    DoConnect();
                    try
                    {
                        entries = DoMsrpcShareEnum();
                    }
                    catch (IOException ioe)
                    {
                        if (Log.Level >= 3)
                        {
                            Runtime.PrintStackTrace(ioe, Log);
                        }
                        entries = DoNetShareEnum();
                    }
                    for (int ei = 0; ei < entries.Length; ei++)
                    {
                        e = entries[ei];
                        if (map.ContainsKey(e) == false)
                        {
                            map.Put(e, e);
                        }
                    }
                    break;
                }
                catch (IOException ioe)
                {
                    if (Log.Level >= 3)
                    {
                        Runtime.PrintStackTrace(ioe, Log);
                    }
                    last = ioe;

                    if (!(ioe is SmbAuthException))
                    {
                        RemoveCurrentAddress();

                        addr = GetNextAddress();
                    }
                    else
                    {
                        break;
                    }
                }

            }
            if (last != null && map.Count == 0)
            {
                if (last is SmbException == false)
                {
                    throw new SmbException(Url.ToString(), last);
                }
                throw (SmbException)last;
            }
            //Iterator iter = map.Keys.Iterator();
            //while (iter.HasNext())
            foreach (var item in map.Keys)
            {
                e = (IFileEntry)item;
                string name = e.GetName();
                if (fnf != null && fnf.Accept(this, name) == false)
                {
                    continue;
                }
                if (name.Length > 0)
                {
                    // if !files we don't need to create SmbFiles here
                    SmbFile f = new SmbFile(this, name, e.GetType(), AttrReadonly
                         | AttrDirectory, 0L, 0L, 0L);
                    if (ff != null && ff.Accept(f) == false)
                    {
                        continue;
                    }
                    if (files)
                    {
                        list.Add(f);
                    }
                    else
                    {
                        list.Add(name);
                    }
                }
            }
        }

        /// <exception cref="System.IO.IOException"></exception>
        internal virtual IFileEntry[] DoDfsRootEnum()
        {
            MsrpcDfsRootEnum rpc;
            DcerpcHandle handle = null;
            IFileEntry[] entries;
            handle = DcerpcHandle.GetHandle("ncacn_np:" + GetAddress().GetHostAddress() + "[\\PIPE\\netdfs]"
                , Auth);
            try
            {
                rpc = new MsrpcDfsRootEnum(GetServer());
                handle.Sendrecv(rpc);
                if (rpc.Retval != 0)
                {
                    throw new SmbException(rpc.Retval, true);
                }
                return rpc.GetEntries();
            }
            finally
            {
                try
                {
                    handle.Close();
                }
                catch (IOException ioe)
                {
                    if (Log.Level >= 4)
                    {
                        Runtime.PrintStackTrace(ioe, Log);
                    }
                }
            }
        }

        /// <exception cref="System.IO.IOException"></exception>
        internal virtual IFileEntry[] DoMsrpcShareEnum()
        {
            MsrpcShareEnum rpc;
            DcerpcHandle handle;
            rpc = new MsrpcShareEnum(Url.GetHost());
            handle = DcerpcHandle.GetHandle("ncacn_np:" + GetAddress().GetHostAddress() + "[\\PIPE\\srvsvc]"
                , Auth);
            try
            {
                handle.Sendrecv(rpc);
                if (rpc.Retval != 0)
                {
                    throw new SmbException(rpc.Retval, true);
                }
                return rpc.GetEntries();
            }
            finally
            {
                try
                {
                    handle.Close();
                }
                catch (IOException ioe)
                {
                    if (Log.Level >= 4)
                    {
                        Runtime.PrintStackTrace(ioe, Log);
                    }
                }
            }
        }

        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        internal virtual IFileEntry[] DoNetShareEnum()
        {
            SmbComTransaction req = new NetShareEnum();
            SmbComTransactionResponse resp = new NetShareEnumResponse();
            Send(req, resp);
            if (resp.Status != WinError.ErrorSuccess)
            {
                throw new SmbException(resp.Status, true);
            }
            return resp.Results;
        }

        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        /// <exception cref="UnknownHostException"></exception>
        /// <exception cref="System.UriFormatException"></exception>
        internal virtual void DoNetServerEnum(List<object> list, bool files, string wildcard
            , int searchAttributes, ISmbFilenameFilter fnf, ISmbFileFilter ff)
        {
            int listType = Url.GetHost().Length == 0 ? 0 : GetType();
            SmbComTransaction req;
            SmbComTransactionResponse resp;
            if (listType == 0)
            {
                Connect0();
                req = new NetServerEnum2(Tree.Session.transport.Server.OemDomainName, NetServerEnum2
                    .SvTypeDomainEnum);
                resp = new NetServerEnum2Response();
            }
            else
            {
                if (listType == TypeWorkgroup)
                {
                    req = new NetServerEnum2(Url.GetHost(), NetServerEnum2.SvTypeAll);
                    resp = new NetServerEnum2Response();
                }
                else
                {
                    throw new SmbException("The requested list operations is invalid: " + Url);
                }
            }
            bool more;
            do
            {
                int n;
                Send(req, resp);
                if (resp.Status != WinError.ErrorSuccess && resp.Status != WinError.ErrorMoreData)
                {
                    throw new SmbException(resp.Status, true);
                }
                more = resp.Status == WinError.ErrorMoreData;
                n = more ? resp.NumEntries - 1 : resp.NumEntries;
                for (int i = 0; i < n; i++)
                {
                    IFileEntry e = resp.Results[i];
                    string name = e.GetName();
                    if (fnf != null && fnf.Accept(this, name) == false)
                    {
                        continue;
                    }
                    if (name.Length > 0)
                    {
                        // if !files we don't need to create SmbFiles here
                        SmbFile f = new SmbFile(this, name, e.GetType(), AttrReadonly
                             | AttrDirectory, 0L, 0L, 0L);
                        if (ff != null && ff.Accept(f) == false)
                        {
                            continue;
                        }
                        if (files)
                        {
                            list.Add(f);
                        }
                        else
                        {
                            list.Add(name);
                        }
                    }
                }
                if (GetType() != TypeWorkgroup)
                {
                    break;
                }
                req.SubCommand = unchecked(SmbComTransaction.NetServerEnum3);
                req.Reset(0, ((NetServerEnum2Response)resp).LastName);
                resp.Reset();
            }
            while (more);
        }

        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        /// <exception cref="UnknownHostException"></exception>
        /// <exception cref="System.UriFormatException"></exception>
        internal virtual void DoFindFirstNext(List<object> list, bool files, string wildcard
            , int searchAttributes, ISmbFilenameFilter fnf, ISmbFileFilter ff)
        {
            SmbComTransaction req;
            Trans2FindFirst2Response resp;
            int sid;
            string path = GetUncPath0();
            string p = Url.AbsolutePath;
            if (p.LastIndexOf('/') != (p.Length - 1))
            {
                throw new SmbException(Url + " directory must end with '/'");
            }
            req = new Trans2FindFirst2(path, wildcard, searchAttributes);
            resp = new Trans2FindFirst2Response();
            if (Log.Level >= 3)
            {
                Log.WriteLine("doFindFirstNext: " + req.Path);
            }
            Send(req, resp);
            sid = resp.Sid;
            req = new Trans2FindNext2(sid, resp.ResumeKey, resp.LastName);
            resp.SubCommand = SmbComTransaction.Trans2FindNext2;
            for (; ; )
            {
                for (int i = 0; i < resp.NumEntries; i++)
                {
                    IFileEntry e = resp.Results[i];
                    string name = e.GetName();
                    if (name.Length < 3)
                    {
                        int h = name.GetHashCode();
                        if (h == HashDot || h == HashDotDot)
                        {
                            if (name.Equals(".") || name.Equals(".."))
                            {
                                continue;
                            }
                        }
                    }
                    if (fnf != null && fnf.Accept(this, name) == false)
                    {
                        continue;
                    }
                    if (name.Length > 0)
                    {
                        SmbFile f = new SmbFile(this, name, TypeFilesystem, e.GetAttributes
                            (), e.CreateTime(), e.LastModified(), e.Length());
                        if (ff != null && ff.Accept(f) == false)
                        {
                            continue;
                        }
                        if (files)
                        {
                            list.Add(f);
                        }
                        else
                        {
                            list.Add(name);
                        }
                    }
                }
                if (resp.IsEndOfSearch || resp.NumEntries == 0)
                {
                    break;
                }
                req.Reset(resp.ResumeKey, resp.LastName);
                resp.Reset();
                Send(req, resp);
            }
            try
            {
                Send(new SmbComFindClose2(sid), Blank_resp());
            }
            catch (SmbException se)
            {
                if (Log.Level >= 4)
                {
                    Runtime.PrintStackTrace(se, Log);
                }
            }
        }

        /// <summary>
        /// Changes the name of the file this <code>SmbFile</code> represents to the name
        /// designated by the <code>SmbFile</code> argument.
        /// </summary>
        /// <remarks>
        /// Changes the name of the file this <code>SmbFile</code> represents to the name
        /// designated by the <code>SmbFile</code> argument.
        /// <p/>
        /// <i>Remember: <code>SmbFile</code>s are immutible and therefore
        /// the path associated with this <code>SmbFile</code> object will not
        /// change). To access the renamed file it is necessary to construct a
        /// new <tt>SmbFile</tt></i>.
        /// </remarks>
        /// <param name="dest">An <code>SmbFile</code> that represents the new pathname</param>
        /// <exception cref="System.ArgumentNullException">If the <code>dest</code> argument is <code>null</code>
        /// 	</exception>
        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        public virtual void RenameTo(SmbFile dest)
        {
            if (GetUncPath0().Length == 1 || dest.GetUncPath0().Length == 1)
            {
                throw new SmbException("Invalid operation for workgroups, servers, or shares");
            }
            ResolveDfs(null);
            dest.ResolveDfs(null);
            if (!Tree.Equals(dest.Tree))
            {
                throw new SmbException("Invalid operation for workgroups, servers, or shares");
            }
            if (Log.Level >= 3)
            {
                Log.WriteLine("renameTo: " + Unc + " -> " + dest.Unc);
            }
            _attrExpiration = _sizeExpiration = 0;
            dest._attrExpiration = 0;
            Send(new SmbComRename(Unc, dest.Unc), Blank_resp());
        }

        internal class WriterThread : Thread
        {
            internal byte[] B;

            internal int N;

            internal long Off;

            internal bool Ready;

            internal SmbFile Dest;

            internal SmbException E;

            internal bool UseNtSmbs;

            internal SmbComWriteAndX Reqx;

            internal SmbComWrite Req;

            internal ServerMessageBlock Resp;

            /// <exception cref="SharpCifs.Smb.SmbException"></exception>
            public WriterThread(SmbFile enclosing)
                : base("JCIFS-WriterThread")
            {
                this._enclosing = enclosing;
                UseNtSmbs = this._enclosing.Tree.Session.transport.HasCapability(SmbConstants.CapNtSmbs);
                if (UseNtSmbs)
                {
                    Reqx = new SmbComWriteAndX();
                    Resp = new SmbComWriteAndXResponse();
                }
                else
                {
                    Req = new SmbComWrite();
                    Resp = new SmbComWriteResponse();
                }
                Ready = false;
            }

            internal virtual void Write(byte[] b, int n, SmbFile dest, long off)
            {
                lock (this)
                {
                    this.B = b;
                    this.N = n;
                    this.Dest = dest;
                    this.Off = off;
                    Ready = false;
                    Runtime.Notify(this);
                }
            }

            public override void Run()
            {
                lock (this)
                {
                    try
                    {
                        for (; ; )
                        {
                            Runtime.Notify(this);
                            Ready = true;
                            while (Ready)
                            {
                                Runtime.Wait(this);
                            }
                            if (N == -1)
                            {
                                return;
                            }
                            if (UseNtSmbs)
                            {
                                Reqx.SetParam(Dest.Fid, Off, N, B, 0, N);
                                Dest.Send(Reqx, Resp);
                            }
                            else
                            {
                                Req.SetParam(Dest.Fid, Off, N, B, 0, N);
                                Dest.Send(Req, Resp);
                            }
                        }
                    }
                    catch (SmbException e)
                    {
                        this.E = e;
                    }
                    catch (Exception x)
                    {
                        E = new SmbException("WriterThread", x);
                    }
                    Runtime.Notify(this);
                }
            }

            private readonly SmbFile _enclosing;
        }

        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        internal virtual void CopyTo0(SmbFile dest, byte[][] b, int bsize, WriterThread
             w, SmbComReadAndX req, SmbComReadAndXResponse resp)
        {
            int i;
            if (_attrExpiration < Runtime.CurrentTimeMillis())
            {
                _attributes = AttrReadonly | AttrDirectory;
                _createTime = 0L;
                _lastModified = 0L;
                _isExists = false;
                IInfo info = QueryPath(GetUncPath0(), Trans2QueryPathInformationResponse.SMB_QUERY_FILE_BASIC_INFO
                    );
                _attributes = info.GetAttributes();
                _createTime = info.GetCreateTime();
                _lastModified = info.GetLastWriteTime();
                _isExists = true;
                _attrExpiration = Runtime.CurrentTimeMillis() + AttrExpirationPeriod;
            }
            if (IsDirectory())
            {
                SmbFile[] files;
                SmbFile ndest;
                string path = dest.GetUncPath0();
                if (path.Length > 1)
                {
                    try
                    {
                        dest.Mkdir();
                        dest.SetPathInformation(_attributes, _createTime, _lastModified);
                    }
                    catch (SmbException se)
                    {
                        if (se.GetNtStatus() != NtStatus.NtStatusAccessDenied && se.GetNtStatus() != NtStatus
                            .NtStatusObjectNameCollision)
                        {
                            throw;
                        }
                    }
                }
                files = ListFiles("*", AttrDirectory | AttrHidden | AttrSystem, null, null);
                try
                {
                    for (i = 0; i < files.Length; i++)
                    {
                        ndest = new SmbFile(dest, files[i].GetName(), files[i].Type, files[i]._attributes,
                            files[i]._createTime, files[i]._lastModified, files[i]._size);
                        files[i].CopyTo0(ndest, b, bsize, w, req, resp);
                    }
                }
                catch (UnknownHostException uhe)
                {
                    throw new SmbException(Url.ToString(), uhe);
                }
                catch (UriFormatException mue)
                {
                    throw new SmbException(Url.ToString(), mue);
                }
            }
            else
            {
                long off;
                try
                {
                    Open(ORdonly, 0, AttrNormal, 0);
                    try
                    {
                        dest.Open(OCreat | OWronly | OTrunc, SmbConstants.FileWriteData |
                             SmbConstants.FileWriteAttributes, _attributes, 0);
                    }
                    catch (SmbAuthException sae)
                    {
                        if ((dest._attributes & AttrReadonly) != 0)
                        {
                            dest.SetPathInformation(dest._attributes & ~AttrReadonly, 0L, 0L);
                            dest.Open(OCreat | OWronly | OTrunc, SmbConstants.FileWriteData |
                                 SmbConstants.FileWriteAttributes, _attributes, 0);
                        }
                        else
                        {
                            throw;
                        }
                    }
                    i = 0;
                    off = 0L;
                    for (; ; )
                    {
                        req.SetParam(Fid, off, bsize);
                        resp.SetParam(b[i], 0);
                        Send(req, resp);
                        lock (w)
                        {
                            if (w.E != null)
                            {
                                throw w.E;
                            }
                            while (!w.Ready)
                            {
                                try
                                {
                                    Runtime.Wait(w);
                                }
                                catch (Exception ie)
                                {
                                    throw new SmbException(dest.Url.ToString(), ie);
                                }
                            }
                            if (w.E != null)
                            {
                                throw w.E;
                            }
                            if (resp.DataLength <= 0)
                            {
                                break;
                            }
                            w.Write(b[i], resp.DataLength, dest, off);
                        }
                        i = i == 1 ? 0 : 1;
                        off += resp.DataLength;
                    }
                    dest.Send(new Trans2SetFileInformation(dest.Fid, _attributes, _createTime, _lastModified
                        ), new Trans2SetFileInformationResponse());
                    dest.Close(0L);
                }
                catch (SmbException se)
                {
                    if (IgnoreCopyToException == false)
                    {
                        throw new SmbException("Failed to copy file from [" + ToString() + "] to ["
                            + dest + "]", se);
                    }
                    if (Log.Level > 1)
                    {
                        Runtime.PrintStackTrace(se, Log);
                    }
                }
                finally
                {
                    Close();
                }
            }
        }

        /// <summary>
        /// This method will copy the file or directory represented by this
        /// <tt>SmbFile</tt> and it's sub-contents to the location specified by the
        /// <tt>dest</tt> parameter.
        /// </summary>
        /// <remarks>
        /// This method will copy the file or directory represented by this
        /// <tt>SmbFile</tt> and it's sub-contents to the location specified by the
        /// <tt>dest</tt> parameter. This file and the destination file do not
        /// need to be on the same host. This operation does not copy extended
        /// file attibutes such as ACLs but it does copy regular attributes as
        /// well as create and last write times. This method is almost twice as
        /// efficient as manually copying as it employs an additional write
        /// thread to read and write data concurrently.
        /// <p/>
        /// It is not possible (nor meaningful) to copy entire workgroups or
        /// servers.
        /// </remarks>
        /// <param name="dest">the destination file or directory</param>
        /// <exception cref="SmbException">SmbException</exception>
        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        public virtual void CopyTo(SmbFile dest)
        {
            SmbComReadAndX req;
            SmbComReadAndXResponse resp;
            WriterThread w;
            int bsize;
            byte[][] b;
            if (_share == null || dest._share == null)
            {
                throw new SmbException("Invalid operation for workgroups or servers");
            }
            req = new SmbComReadAndX();
            resp = new SmbComReadAndXResponse();
            Connect0();
            dest.Connect0();
            ResolveDfs(null);
            try
            {
                if (GetAddress().Equals(dest.GetAddress()) && _canon.RegionMatches(true, 0, dest._canon
                    , 0, Math.Min(_canon.Length, dest._canon.Length)))
                {
                    throw new SmbException("Source and destination paths overlap.");
                }
            }
            catch (UnknownHostException)
            {
            }
            w = new WriterThread(this);
            w.SetDaemon(true);
            w.Start();
            SmbTransport t1 = Tree.Session.transport;
            SmbTransport t2 = dest.Tree.Session.transport;
            if (t1.SndBufSize < t2.SndBufSize)
            {
                t2.SndBufSize = t1.SndBufSize;
            }
            else
            {
                t1.SndBufSize = t2.SndBufSize;
            }
            bsize = Math.Min(t1.RcvBufSize - 70, t1.SndBufSize - 70);
            b = new[] { new byte[bsize], new byte[bsize] };
            try
            {
                CopyTo0(dest, b, bsize, w, req, resp);
            }
            finally
            {
                w.Write(null, -1, null, 0);
            }
        }

        /// <summary>
        /// This method will delete the file or directory specified by this
        /// <code>SmbFile</code>.
        /// </summary>
        /// <remarks>
        /// This method will delete the file or directory specified by this
        /// <code>SmbFile</code>. If the target is a directory, the contents of
        /// the directory will be deleted as well. If a file within the directory or
        /// it's sub-directories is marked read-only, the read-only status will
        /// be removed and the file will be deleted.
        /// </remarks>
        /// <exception cref="SmbException">SmbException</exception>
        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        public virtual void Delete()
        {
            Exists();
            GetUncPath0();
            Delete(Unc);
        }

        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        internal virtual void Delete(string fileName)
        {
            if (GetUncPath0().Length == 1)
            {
                throw new SmbException("Invalid operation for workgroups, servers, or shares");
            }
            if (Runtime.CurrentTimeMillis() > _attrExpiration)
            {
                _attributes = AttrReadonly | AttrDirectory;
                _createTime = 0L;
                _lastModified = 0L;
                _isExists = false;
                IInfo info = QueryPath(GetUncPath0(), Trans2QueryPathInformationResponse.SMB_QUERY_FILE_BASIC_INFO
                    );
                _attributes = info.GetAttributes();
                _createTime = info.GetCreateTime();
                _lastModified = info.GetLastWriteTime();
                _attrExpiration = Runtime.CurrentTimeMillis() + AttrExpirationPeriod;
                _isExists = true;
            }
            if ((_attributes & AttrReadonly) != 0)
            {
                SetReadWrite();
            }
            if (Log.Level >= 3)
            {
                Log.WriteLine("delete: " + fileName);
            }
            if ((_attributes & AttrDirectory) != 0)
            {
                try
                {
                    SmbFile[] l = ListFiles("*", AttrDirectory | AttrHidden | AttrSystem, null, null
                        );
                    for (int i = 0; i < l.Length; i++)
                    {
                        l[i].Delete();
                    }
                }
                catch (SmbException se)
                {
                    if (se.GetNtStatus() != NtStatus.NtStatusNoSuchFile)
                    {
                        throw;
                    }
                }
                Send(new SmbComDeleteDirectory(fileName), Blank_resp());
            }
            else
            {
                Send(new SmbComDelete(fileName), Blank_resp());
            }
            _attrExpiration = _sizeExpiration = 0;
        }

        /// <summary>Returns the length of this <tt>SmbFile</tt> in bytes.</summary>
        /// <remarks>
        /// Returns the length of this <tt>SmbFile</tt> in bytes. If this object
        /// is a <tt>TYPE_SHARE</tt> the total capacity of the disk shared in
        /// bytes is returned. If this object is a directory or a type other than
        /// <tt>TYPE_SHARE</tt>, 0L is returned.
        /// </remarks>
        /// <returns>
        /// The length of the file in bytes or 0 if this
        /// <code>SmbFile</code> is not a file.
        /// </returns>
        /// <exception cref="SmbException">SmbException</exception>
        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        public virtual long Length()
        {
            if (_sizeExpiration > Runtime.CurrentTimeMillis())
            {
                return _size;
            }
            if (GetType() == TypeShare)
            {
                Trans2QueryFsInformationResponse response;
                int level = Trans2QueryFsInformationResponse.SMB_INFO_ALLOCATION;
                response = new Trans2QueryFsInformationResponse(level);
                Send(new Trans2QueryFsInformation(level), response);
                _size = response.Info.GetCapacity();
            }
            else
            {
                if (GetUncPath0().Length > 1 && Type != TypeNamedPipe)
                {
                    IInfo info = QueryPath(GetUncPath0(), Trans2QueryPathInformationResponse.SMB_QUERY_FILE_STANDARD_INFO
                        );
                    _size = info.GetSize();
                }
                else
                {
                    _size = 0L;
                }
            }
            _sizeExpiration = Runtime.CurrentTimeMillis() + AttrExpirationPeriod;
            return _size;
        }

        /// <summary>
        /// This method returns the free disk space in bytes of the drive this share
        /// represents or the drive on which the directory or file resides.
        /// </summary>
        /// <remarks>
        /// This method returns the free disk space in bytes of the drive this share
        /// represents or the drive on which the directory or file resides. Objects
        /// other than <tt>TYPE_SHARE</tt> or <tt>TYPE_FILESYSTEM</tt> will result
        /// in 0L being returned.
        /// </remarks>
        /// <returns>
        /// the free disk space in bytes of the drive on which this file or
        /// directory resides
        /// </returns>
        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        public virtual long GetDiskFreeSpace()
        {
            if (GetType() == TypeShare || Type == TypeFilesystem)
            {
                int level = Trans2QueryFsInformationResponse.SmbFsFullSizeInformation;
                try
                {
                    return QueryFsInformation(level);
                }
                catch (SmbException ex)
                {
                    switch (ex.GetNtStatus())
                    {
                        case NtStatus.NtStatusInvalidInfoClass:
                        case NtStatus.NtStatusUnsuccessful:
                            {
                                // NetApp Filer
                                // SMB_FS_FULL_SIZE_INFORMATION not supported by the server.
                                level = Trans2QueryFsInformationResponse.SMB_INFO_ALLOCATION;
                                return QueryFsInformation(level);
                            }
                    }
                    throw;
                }
            }
            return 0L;
        }

        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        private long QueryFsInformation(int level)
        {
            Trans2QueryFsInformationResponse response;
            response = new Trans2QueryFsInformationResponse(level);
            Send(new Trans2QueryFsInformation(level), response);
            if (Type == TypeShare)
            {
                _size = response.Info.GetCapacity();
                _sizeExpiration = Runtime.CurrentTimeMillis() + AttrExpirationPeriod;
            }
            return response.Info.GetFree();
        }

        /// <summary>
        /// Creates a directory with the path specified by this
        /// <code>SmbFile</code>.
        /// </summary>
        /// <remarks>
        /// Creates a directory with the path specified by this
        /// <code>SmbFile</code>. For this method to be successful, the target
        /// must not already exist. This method will fail when
        /// used with <code>smb://</code>, <code>smb://workgroup/</code>,
        /// <code>smb://server/</code>, or <code>smb://server/share/</code> URLs
        /// because workgroups, servers, and shares cannot be dynamically created
        /// (although in the future it may be possible to create shares).
        /// </remarks>
        /// <exception cref="SmbException">SmbException</exception>
        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        public virtual void Mkdir()
        {
            string path = GetUncPath0();
            if (path.Length == 1)
            {
                throw new SmbException("Invalid operation for workgroups, servers, or shares");
            }
            if (Log.Level >= 3)
            {
                Log.WriteLine("mkdir: " + path);
            }
            Send(new SmbComCreateDirectory(path), Blank_resp());
            _attrExpiration = _sizeExpiration = 0;
        }

        /// <summary>
        /// Creates a directory with the path specified by this <tt>SmbFile</tt>
        /// and any parent directories that do not exist.
        /// </summary>
        /// <remarks>
        /// Creates a directory with the path specified by this <tt>SmbFile</tt>
        /// and any parent directories that do not exist. This method will fail
        /// when used with <code>smb://</code>, <code>smb://workgroup/</code>,
        /// <code>smb://server/</code>, or <code>smb://server/share/</code> URLs
        /// because workgroups, servers, and shares cannot be dynamically created
        /// (although in the future it may be possible to create shares).
        /// </remarks>
        /// <exception cref="SmbException">SmbException</exception>
        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        public virtual void Mkdirs()
        {
            SmbFile parent;
            try
            {
                parent = new SmbFile(GetParent(), Auth);
            }
            catch (IOException)
            {
                return;
            }
            if (parent.Exists() == false)
            {
                parent.Mkdirs();
            }
            Mkdir();
        }

        /// <summary>Create a new file but fail if it already exists.</summary>
        /// <remarks>
        /// Create a new file but fail if it already exists. The check for
        /// existance of the file and it's creation are an atomic operation with
        /// respect to other filesystem activities.
        /// </remarks>
        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        public virtual void CreateNewFile()
        {
            if (GetUncPath0().Length == 1)
            {
                throw new SmbException("Invalid operation for workgroups, servers, or shares");
            }
            Close(Open0(ORdwr | OCreat | OExcl, 0, AttrNormal, 0), 0L);
        }

        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        internal virtual void SetPathInformation(int attrs, long ctime, long mtime)
        {
            int f;
            int dir;
            Exists();
            dir = _attributes & AttrDirectory;
            f = Open0(ORdonly, SmbConstants.FileWriteAttributes, dir, dir != 0 ? 0x0001
                 : 0x0040);
            Send(new Trans2SetFileInformation(f, attrs | dir, ctime, mtime), new Trans2SetFileInformationResponse
                ());
            Close(f, 0L);
            _attrExpiration = 0;
        }

        /// <summary>Set the create time of the file.</summary>
        /// <remarks>
        /// Set the create time of the file. The time is specified as milliseconds
        /// from Jan 1, 1970 which is the same as that which is returned by the
        /// <tt>createTime()</tt> method.
        /// <p/>
        /// This method does not apply to workgroups, servers, or shares.
        /// </remarks>
        /// <param name="time">the create time as milliseconds since Jan 1, 1970</param>
        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        public virtual void SetCreateTime(long time)
        {
            if (GetUncPath0().Length == 1)
            {
                throw new SmbException("Invalid operation for workgroups, servers, or shares");
            }
            SetPathInformation(0, time, 0L);
        }

        /// <summary>Set the last modified time of the file.</summary>
        /// <remarks>
        /// Set the last modified time of the file. The time is specified as milliseconds
        /// from Jan 1, 1970 which is the same as that which is returned by the
        /// <tt>lastModified()</tt>, <tt>getLastModified()</tt>, and <tt>getDate()</tt> methods.
        /// <p/>
        /// This method does not apply to workgroups, servers, or shares.
        /// </remarks>
        /// <param name="time">the last modified time as milliseconds since Jan 1, 1970</param>
        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        public virtual void SetLastModified(long time)
        {
            if (GetUncPath0().Length == 1)
            {
                throw new SmbException("Invalid operation for workgroups, servers, or shares");
            }
            SetPathInformation(0, 0L, time);
        }

        /// <summary>Return the attributes of this file.</summary>
        /// <remarks>
        /// Return the attributes of this file. Attributes are represented as a
        /// bitset that must be masked with <tt>ATTR_*</tt> constants to determine
        /// if they are set or unset. The value returned is suitable for use with
        /// the <tt>setAttributes()</tt> method.
        /// </remarks>
        /// <returns>the <tt>ATTR_*</tt> attributes associated with this file</returns>
        /// <exception cref="SmbException">SmbException</exception>
        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        public virtual int GetAttributes()
        {
            if (GetUncPath0().Length == 1)
            {
                return 0;
            }
            Exists();
            return _attributes & AttrGetMask;
        }

        /// <summary>Set the attributes of this file.</summary>
        /// <remarks>
        /// Set the attributes of this file. Attributes are composed into a
        /// bitset by bitwise ORing the <tt>ATTR_*</tt> constants. Setting the
        /// value returned by <tt>getAttributes</tt> will result in both files
        /// having the same attributes.
        /// </remarks>
        /// <exception cref="SmbException">SmbException</exception>
        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        public virtual void SetAttributes(int attrs)
        {
            if (GetUncPath0().Length == 1)
            {
                throw new SmbException("Invalid operation for workgroups, servers, or shares");
            }
            SetPathInformation(attrs & AttrSetMask, 0L, 0L);
        }

        /// <summary>Make this file read-only.</summary>
        /// <remarks>
        /// Make this file read-only. This is shorthand for <tt>setAttributes(
        /// getAttributes() | ATTR_READ_ONLY )</tt>.
        /// </remarks>
        /// <exception cref="SmbException">SmbException</exception>
        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        public virtual void SetReadOnly()
        {
            SetAttributes(GetAttributes() | AttrReadonly);
        }

        /// <summary>Turn off the read-only attribute of this file.</summary>
        /// <remarks>
        /// Turn off the read-only attribute of this file. This is shorthand for
        /// <tt>setAttributes( getAttributes() & ~ATTR_READONLY )</tt>.
        /// </remarks>
        /// <exception cref="SmbException">SmbException</exception>
        /// <exception cref="SmbException"></exception>
        public virtual void SetReadWrite()
        {
            SetAttributes(GetAttributes() & ~AttrReadonly);
        }

        /// <summary>
        /// Returns a
        /// <see cref="System.Uri">System.Uri</see>
        /// for this <code>SmbFile</code>. The
        /// <code>URL</code> may be used as any other <code>URL</code> might to
        /// access an SMB resource. Currently only retrieving data and information
        /// is supported (i.e. no <tt>doOutput</tt>).
        /// </summary>
        /// <returns>
        /// A new <code>
        /// <see cref="System.Uri">System.Uri</see>
        /// </code> for this <code>SmbFile</code>
        /// </returns>
        /// <exception cref="System.UriFormatException">System.UriFormatException</exception>
        [Obsolete(@"Use getURL() instead")]
        public virtual Uri ToUrl()
        {
            return Url;
        }

        /// <summary>
        /// Computes a hashCode for this file based on the URL string and IP
        /// address if the server.
        /// </summary>
        /// <remarks>
        /// Computes a hashCode for this file based on the URL string and IP
        /// address if the server. The hashing function uses the hashcode of the
        /// server address, the canonical representation of the URL, and does not
        /// compare authentication information. In essance, two
        /// <code>SmbFile</code> objects that refer to
        /// the same file should generate the same hashcode provided it is possible
        /// to make such a determination.
        /// </remarks>
        /// <returns>A hashcode for this abstract file</returns>
        /// <exception cref="SmbException">SmbException</exception>
        public override int GetHashCode()
        {
            int hash;
            try
            {
                hash = GetAddress().GetHashCode();
            }
            catch (UnknownHostException)
            {
                hash = GetServer().ToUpper().GetHashCode();
            }
            GetUncPath0();
            return hash + _canon.ToUpper().GetHashCode();
        }

        protected internal virtual bool PathNamesPossiblyEqual(string path1, string path2
            )
        {
            int p1;
            int p2;
            int l1;
            int l2;
            // if unsure return this method returns true
            p1 = path1.LastIndexOf('/');
            p2 = path2.LastIndexOf('/');
            l1 = path1.Length - p1;
            l2 = path2.Length - p2;
            // anything with dots voids comparison
            if (l1 > 1 && path1[p1 + 1] == '.')
            {
                return true;
            }
            if (l2 > 1 && path2[p2 + 1] == '.')
            {
                return true;
            }
            return l1 == l2 && path1.RegionMatches(true, p1, path2, p2, l1);
        }

        /// <summary>Tests to see if two <code>SmbFile</code> objects are equal.</summary>
        /// <remarks>
        /// Tests to see if two <code>SmbFile</code> objects are equal. Two
        /// SmbFile objects are equal when they reference the same SMB
        /// resource. More specifically, two <code>SmbFile</code> objects are
        /// equals if their server IP addresses are equal and the canonicalized
        /// representation of their URLs, minus authentication parameters, are
        /// case insensitivly and lexographically equal.
        /// <p/>
        /// For example, assuming the server <code>angus</code> resolves to the
        /// <code>192.168.1.15</code> IP address, the below URLs would result in
        /// <code>SmbFile</code>s that are equal.
        /// <p><blockquote><pre>
        /// smb://192.168.1.15/share/DIR/foo.txt
        /// smb://angus/share/data/../dir/foo.txt
        /// </pre></blockquote>
        /// </remarks>
        /// <param name="obj">Another <code>SmbFile</code> object to compare for equality</param>
        /// <returns>
        /// <code>true</code> if the two objects refer to the same SMB resource
        /// and <code>false</code> otherwise
        /// </returns>
        /// <exception cref="SmbException">SmbException</exception>
        public override bool Equals(object obj)
        {
            if (obj is SmbFile)
            {
                SmbFile f = (SmbFile)obj;
                bool ret;
                if (this == f)
                {
                    return true;
                }
                if (PathNamesPossiblyEqual(Url.AbsolutePath, f.Url.AbsolutePath))
                {
                    GetUncPath0();
                    f.GetUncPath0();
                    if (Runtime.EqualsIgnoreCase(_canon, f._canon))
                    {
                        try
                        {
                            ret = GetAddress().Equals(f.GetAddress());
                        }
                        catch (UnknownHostException)
                        {
                            ret = Runtime.EqualsIgnoreCase(GetServer(), f.GetServer());
                        }
                        return ret;
                    }
                }
            }
            return false;
        }

        /// <summary>Returns the string representation of this SmbFile object.</summary>
        /// <remarks>
        /// Returns the string representation of this SmbFile object. This will
        /// be the same as the URL used to construct this <code>SmbFile</code>.
        /// This method will return the same value
        /// as <code>getPath</code>.
        /// </remarks>
        /// <returns>The original URL representation of this SMB resource</returns>
        /// <exception cref="SmbException">SmbException</exception>
        public override string ToString()
        {
            return Url.ToString();
        }

        /// <summary>This URLConnection method just returns the result of <tt>length()</tt>.</summary>
        /// <remarks>This URLConnection method just returns the result of <tt>length()</tt>.</remarks>
        /// <returns>the length of this file or 0 if it refers to a directory</returns>
        public int GetContentLength()
        {
            try
            {
                return (int)(Length() & unchecked(0xFFFFFFFFL));
            }
            catch (SmbException)
            {
            }
            return 0;
        }

        /// <summary>This URLConnection method just returns the result of <tt>lastModified</tt>.
        /// 	</summary>
        /// <remarks>This URLConnection method just returns the result of <tt>lastModified</tt>.
        /// 	</remarks>
        /// <returns>the last modified data as milliseconds since Jan 1, 1970</returns>
        public long GetDate()
        {
            try
            {
                return LastModified();
            }
            catch (SmbException)
            {
            }
            return 0L;
        }

        /// <summary>This URLConnection method just returns the result of <tt>lastModified</tt>.
        /// 	</summary>
        /// <remarks>This URLConnection method just returns the result of <tt>lastModified</tt>.
        /// 	</remarks>
        /// <returns>the last modified data as milliseconds since Jan 1, 1970</returns>
        public long GetLastModified()
        {
            try
            {
                return LastModified();
            }
            catch (SmbException)
            {
            }
            return 0L;
        }

        /// <summary>This URLConnection method just returns a new <tt>SmbFileInputStream</tt> created with this file.
        /// 	</summary>
        /// <remarks>This URLConnection method just returns a new <tt>SmbFileInputStream</tt> created with this file.
        /// 	</remarks>
        /// <exception cref="System.IO.IOException">thrown by <tt>SmbFileInputStream</tt> constructor
        /// 	</exception>
        public InputStream GetInputStream()
        {
            return new SmbFileInputStream(this);
        }

        /// <summary>This URLConnection method just returns a new <tt>SmbFileOutputStream</tt> created with this file.
        /// 	</summary>
        /// <remarks>This URLConnection method just returns a new <tt>SmbFileOutputStream</tt> created with this file.
        /// 	</remarks>
        /// <exception cref="System.IO.IOException">thrown by <tt>SmbFileOutputStream</tt> constructor
        /// 	</exception>
        public OutputStream GetOutputStream()
        {
            return new SmbFileOutputStream(this);
        }

        /// <exception cref="System.IO.IOException"></exception>
        private void ProcessAces(Ace[] aces, bool resolveSids)
        {
            string server = GetServerWithDfs();
            int ai;
            if (resolveSids)
            {
                Sid[] sids = new Sid[aces.Length];
                string[] names = null;
                for (ai = 0; ai < aces.Length; ai++)
                {
                    sids[ai] = aces[ai].Sid;
                }
                for (int off = 0; off < sids.Length; off += 64)
                {
                    int len = sids.Length - off;
                    if (len > 64)
                    {
                        len = 64;
                    }
                    Sid.ResolveSids(server, Auth, sids, off, len);
                }
            }
            else
            {
                for (ai = 0; ai < aces.Length; ai++)
                {
                    aces[ai].Sid.OriginServer = server;
                    aces[ai].Sid.OriginAuth = Auth;
                }
            }
        }

        /// <summary>
        /// Return an array of Access Control Entry (ACE) objects representing
        /// the security descriptor associated with this file or directory.
        /// </summary>
        /// <remarks>
        /// Return an array of Access Control Entry (ACE) objects representing
        /// the security descriptor associated with this file or directory.
        /// If no DACL is present, null is returned. If the DACL is empty, an array with 0 elements is returned.
        /// </remarks>
        /// <param name="resolveSids">
        /// Attempt to resolve the SIDs within each ACE form
        /// their numeric representation to their corresponding account names.
        /// </param>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual Ace[] GetSecurity(bool resolveSids)
        {
            int f;
            Ace[] aces;
            f = Open0(ORdonly, SmbConstants.ReadControl, 0, IsDirectory() ? 1 : 0);
            NtTransQuerySecurityDesc request = new NtTransQuerySecurityDesc(f, 0x04);
            NtTransQuerySecurityDescResponse response = new NtTransQuerySecurityDescResponse(
                );
            try
            {
                Send(request, response);
            }
            finally
            {
                Close(f, 0L);
            }
            aces = response.SecurityDescriptor.Aces;
            if (aces != null)
            {
                ProcessAces(aces, resolveSids);
            }
            return aces;
        }

        /// <summary>
        /// Return an array of Access Control Entry (ACE) objects representing
        /// the share permissions on the share exporting this file or directory.
        /// </summary>
        /// <remarks>
        /// Return an array of Access Control Entry (ACE) objects representing
        /// the share permissions on the share exporting this file or directory.
        /// If no DACL is present, null is returned. If the DACL is empty, an array with 0 elements is returned.
        /// <p>
        /// Note that this is different from calling <tt>getSecurity</tt> on a
        /// share. There are actually two different ACLs for shares - the ACL on
        /// the share and the ACL on the folder being shared.
        /// Go to <i>Computer Management</i>
        /// &gt; <i>System Tools</i> &gt; <i>Shared Folders</i> &gt <i>Shares</i> and
        /// look at the <i>Properties</i> for a share. You will see two tabs - one
        /// for "Share Permissions" and another for "Security". These correspond to
        /// the ACLs returned by <tt>getShareSecurity</tt> and <tt>getSecurity</tt>
        /// respectively.
        /// </remarks>
        /// <param name="resolveSids">
        /// Attempt to resolve the SIDs within each ACE form
        /// their numeric representation to their corresponding account names.
        /// </param>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual Ace[] GetShareSecurity(bool resolveSids)
        {
            string p = Url.AbsolutePath;
            MsrpcShareGetInfo rpc;
            DcerpcHandle handle;
            Ace[] aces;
            ResolveDfs(null);
            string server = GetServerWithDfs();
            rpc = new MsrpcShareGetInfo(server, Tree.Share);
            handle = DcerpcHandle.GetHandle("ncacn_np:" + server + "[\\PIPE\\srvsvc]", Auth);
            try
            {
                handle.Sendrecv(rpc);
                if (rpc.Retval != 0)
                {
                    throw new SmbException(rpc.Retval, true);
                }
                aces = rpc.GetSecurity();
                if (aces != null)
                {
                    ProcessAces(aces, resolveSids);
                }
            }
            finally
            {
                try
                {
                    handle.Close();
                }
                catch (IOException ioe)
                {
                    if (Log.Level >= 1)
                    {
                        Runtime.PrintStackTrace(ioe, Log);
                    }
                }
            }
            return aces;
        }

        /// <summary>
        /// Return an array of Access Control Entry (ACE) objects representing
        /// the security descriptor associated with this file or directory.
        /// </summary>
        /// <remarks>
        /// Return an array of Access Control Entry (ACE) objects representing
        /// the security descriptor associated with this file or directory.
        /// <p>
        /// Initially, the SIDs within each ACE will not be resolved however when
        /// <tt>getType()</tt>, <tt>getDomainName()</tt>, <tt>getAccountName()</tt>,
        /// or <tt>toString()</tt> is called, the names will attempt to be
        /// resolved. If the names cannot be resolved (e.g. due to temporary
        /// network failure), the said methods will return default values (usually
        /// <tt>S-X-Y-Z</tt> strings of fragments of).
        /// <p>
        /// Alternatively <tt>getSecurity(true)</tt> may be used to resolve all
        /// SIDs together and detect network failures.
        /// </remarks>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual Ace[] GetSecurity()
        {
            return GetSecurity(false);
        }
    }
}
