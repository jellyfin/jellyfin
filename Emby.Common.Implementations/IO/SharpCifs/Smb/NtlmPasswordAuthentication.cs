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
using SharpCifs.Util;
using SharpCifs.Util.Sharpen;

namespace SharpCifs.Smb
{
    /// <summary>This class stores and encrypts NTLM user credentials.</summary>
    /// <remarks>
    /// This class stores and encrypts NTLM user credentials. The default
    /// credentials are retrieved from the <tt>jcifs.smb.client.domain</tt>,
    /// <tt>jcifs.smb.client.username</tt>, and <tt>jcifs.smb.client.password</tt>
    /// properties.
    /// <p>
    /// Read <a href="../../../authhandler.html">jCIFS Exceptions and
    /// NtlmAuthenticator</a> for related information.
    /// </remarks>

    public sealed class NtlmPasswordAuthentication : Principal
    {
        private static readonly int LmCompatibility
            = Config.GetInt("jcifs.smb.lmCompatibility", 3);

        private static readonly Random Random = new Random();

        private static LogStream _log = LogStream.GetInstance();

        private static readonly byte[] S8 =
        {
            unchecked(unchecked(0x4b)),
            unchecked(unchecked(0x47)),
            unchecked(unchecked(0x53)),
            unchecked(unchecked(0x21)),
            unchecked(unchecked(0x40)),
            unchecked(unchecked(0x23)),
            unchecked(unchecked(0x24)),
            unchecked(unchecked(0x25))
        };

        // KGS!@#$%
        private static void E(byte[] key, byte[] data, byte[] e)
        {
            byte[] key7 = new byte[7];
            byte[] e8 = new byte[8];
            for (int i = 0; i < key.Length / 7; i++)
            {
                Array.Copy(key, i * 7, key7, 0, 7);
                DES des = new DES(key7);
                des.Encrypt(data, e8);
                Array.Copy(e8, 0, e, i * 8, 8);
            }
        }

        internal static string DefaultDomain;

        internal static string DefaultUsername;

        internal static string DefaultPassword;

        internal static readonly string Blank = string.Empty;

        public static readonly NtlmPasswordAuthentication Anonymous
            = new NtlmPasswordAuthentication(string.Empty, string.Empty, string.Empty);

        internal static void InitDefaults()
        {
            if (DefaultDomain != null)
            {
                return;
            }
            DefaultDomain = Config.GetProperty("jcifs.smb.client.domain", "?");
            DefaultUsername = Config.GetProperty("jcifs.smb.client.username", "GUEST");
            DefaultPassword = Config.GetProperty("jcifs.smb.client.password", Blank);
        }

        /// <summary>
        /// Generate the ANSI DES hash for the password associated with these credentials.
        /// </summary>
        /// <remarks>
        /// Generate the ANSI DES hash for the password associated with these credentials.
        /// </remarks>
        public static byte[] GetPreNtlmResponse(string password, byte[] challenge)
        {
            byte[] p14 = new byte[14];
            byte[] p21 = new byte[21];
            byte[] p24 = new byte[24];
            byte[] passwordBytes;
            try
            {
                passwordBytes = Runtime.GetBytesForString(password.ToUpper(), SmbConstants.OemEncoding);
            }
            catch (UnsupportedEncodingException uee)
            {
                throw new RuntimeException("Try setting jcifs.encoding=US-ASCII", uee);
            }
            int passwordLength = passwordBytes.Length;
            // Only encrypt the first 14 bytes of the password for Pre 0.12 NT LM
            if (passwordLength > 14)
            {
                passwordLength = 14;
            }
            Array.Copy(passwordBytes, 0, p14, 0, passwordLength);
            E(p14, S8, p21);
            E(p21, challenge, p24);
            return p24;
        }

        /// <summary>
        /// Generate the Unicode MD4 hash for the password associated with these credentials.
        /// </summary>
        /// <remarks>
        /// Generate the Unicode MD4 hash for the password associated with these credentials.
        /// </remarks>
        public static byte[] GetNtlmResponse(string password, byte[] challenge)
        {
            byte[] uni = null;
            byte[] p21 = new byte[21];
            byte[] p24 = new byte[24];
            try
            {
                uni = Runtime.GetBytesForString(password, SmbConstants.UniEncoding);
            }
            catch (UnsupportedEncodingException uee)
            {
                if (_log.Level > 0)
                {
                    Runtime.PrintStackTrace(uee, _log);
                }
            }
            Md4 md4 = new Md4();
            md4.Update(uni);
            try
            {
                md4.Digest(p21, 0, 16);
            }
            catch (Exception ex)
            {
                if (_log.Level > 0)
                {
                    Runtime.PrintStackTrace(ex, _log);
                }
            }
            E(p21, challenge, p24);
            return p24;
        }

        /// <summary>Creates the LMv2 response for the supplied information.</summary>
        /// <remarks>Creates the LMv2 response for the supplied information.</remarks>
        /// <param name="domain">The domain in which the username exists.</param>
        /// <param name="user">The username.</param>
        /// <param name="password">The user's password.</param>
        /// <param name="challenge">The server challenge.</param>
        /// <param name="clientChallenge">The client challenge (nonce).</param>
        public static byte[] GetLMv2Response(string domain,
                                             string user,
                                             string password,
                                             byte[] challenge,
                                             byte[] clientChallenge)
        {
            try
            {
                byte[] hash = new byte[16];
                byte[] response = new byte[24];
                // The next 2-1/2 lines of this should be placed with nTOWFv1 in place of password
                Md4 md4 = new Md4();
                md4.Update(Runtime.GetBytesForString(password, SmbConstants.UniEncoding));
                Hmact64 hmac = new Hmact64(md4.Digest());
                hmac.Update(Runtime.GetBytesForString(user.ToUpper(), SmbConstants.UniEncoding));
                hmac.Update(Runtime.GetBytesForString(domain.ToUpper(), SmbConstants.UniEncoding));
                hmac = new Hmact64(hmac.Digest());
                hmac.Update(challenge);
                hmac.Update(clientChallenge);
                hmac.Digest(response, 0, 16);
                Array.Copy(clientChallenge, 0, response, 16, 8);
                return response;
            }
            catch (Exception ex)
            {
                if (_log.Level > 0)
                {
                    Runtime.PrintStackTrace(ex, _log);
                }
                return null;
            }
        }

        public static byte[] GetNtlm2Response(byte[] nTowFv1,
                                              byte[] serverChallenge,
                                              byte[] clientChallenge)
        {
            byte[] sessionHash = new byte[8];
            try
            {
                MessageDigest md5;
                md5 = MessageDigest.GetInstance("MD5");
                md5.Update(serverChallenge);
                md5.Update(clientChallenge, 0, 8);
                Array.Copy(md5.Digest(), 0, sessionHash, 0, 8);
            }
            catch (Exception gse)
            {
                if (_log.Level > 0)
                {
                    Runtime.PrintStackTrace(gse, _log);
                }
                throw new RuntimeException("MD5", gse);
            }
            byte[] key = new byte[21];
            Array.Copy(nTowFv1, 0, key, 0, 16);
            byte[] ntResponse = new byte[24];
            E(key, sessionHash, ntResponse);
            return ntResponse;
        }

        public static byte[] NtowFv1(string password)
        {
            if (password == null)
            {
                throw new RuntimeException("Password parameter is required");
            }
            try
            {
                Md4 md4 = new Md4();
                md4.Update(Runtime.GetBytesForString(password, SmbConstants.UniEncoding));
                return md4.Digest();
            }
            catch (UnsupportedEncodingException uee)
            {
                throw new RuntimeException(uee.Message);
            }
        }

        public static byte[] NtowFv2(string domain, string username, string password)
        {
            try
            {
                Md4 md4 = new Md4();
                md4.Update(Runtime.GetBytesForString(password, SmbConstants.UniEncoding));
                Hmact64 hmac = new Hmact64(md4.Digest());
                hmac.Update(Runtime.GetBytesForString(username.ToUpper(), SmbConstants.UniEncoding));
                hmac.Update(Runtime.GetBytesForString(domain, SmbConstants.UniEncoding));
                return hmac.Digest();
            }
            catch (UnsupportedEncodingException uee)
            {
                throw new RuntimeException(uee.Message);
            }
        }

        internal static byte[] ComputeResponse(byte[] responseKey,
                                               byte[] serverChallenge,
                                               byte[] clientData,
                                               int offset,
                                               int length)
        {
            Hmact64 hmac = new Hmact64(responseKey);
            hmac.Update(serverChallenge);
            hmac.Update(clientData, offset, length);
            byte[] mac = hmac.Digest();
            byte[] ret = new byte[mac.Length + clientData.Length];
            Array.Copy(mac, 0, ret, 0, mac.Length);
            Array.Copy(clientData, 0, ret, mac.Length, clientData.Length);
            return ret;
        }

        public static byte[] GetLMv2Response(byte[] responseKeyLm,
                                             byte[] serverChallenge,
                                             byte[] clientChallenge)
        {
            return ComputeResponse(responseKeyLm,
                                   serverChallenge,
                                   clientChallenge,
                                   0,
                                   clientChallenge.Length);
        }

        public static byte[] GetNtlMv2Response(byte[] responseKeyNt,
                                               byte[] serverChallenge,
                                               byte[] clientChallenge,
                                               long nanos1601,
                                               byte[] targetInfo)
        {
            int targetInfoLength = targetInfo != null
                                        ? targetInfo.Length
                                        : 0;
            byte[] temp = new byte[28 + targetInfoLength + 4];
            Encdec.Enc_uint32le(unchecked(0x00000101), temp, 0);
            // Header
            Encdec.Enc_uint32le(unchecked(0x00000000), temp, 4);
            // Reserved
            Encdec.Enc_uint64le(nanos1601, temp, 8);
            Array.Copy(clientChallenge, 0, temp, 16, 8);
            Encdec.Enc_uint32le(unchecked(0x00000000), temp, 24);
            // Unknown
            if (targetInfo != null)
            {
                Array.Copy(targetInfo, 0, temp, 28, targetInfoLength);
            }
            Encdec.Enc_uint32le(unchecked(0x00000000), temp, 28 + targetInfoLength);
            // mystery bytes!
            return ComputeResponse(responseKeyNt,
                                   serverChallenge,
                                   temp,
                                   0,
                                   temp.Length);
        }

        internal static readonly NtlmPasswordAuthentication Null
            = new NtlmPasswordAuthentication(string.Empty, string.Empty, string.Empty);

        internal static readonly NtlmPasswordAuthentication Guest
            = new NtlmPasswordAuthentication("?", "GUEST", string.Empty);

        internal static readonly NtlmPasswordAuthentication Default
            = new NtlmPasswordAuthentication(null);

        internal string Domain;

        internal string Username;

        internal string Password;

        internal byte[] AnsiHash;

        internal byte[] UnicodeHash;

        internal bool HashesExternal;

        internal byte[] ClientChallenge;

        internal byte[] Challenge;

        /// <summary>
        /// Create an <tt>NtlmPasswordAuthentication</tt> object from the userinfo
        /// component of an SMB URL like "<tt>domain;user:pass</tt>".
        /// </summary>
        /// <remarks>
        /// Create an <tt>NtlmPasswordAuthentication</tt> object from the userinfo
        /// component of an SMB URL like "<tt>domain;user:pass</tt>". This constructor
        /// is used internally be jCIFS when parsing SMB URLs.
        /// </remarks>
        public NtlmPasswordAuthentication(string userInfo)
        {
            Domain = Username = Password = null;
            if (userInfo != null)
            {
                try
                {
                    userInfo = Unescape(userInfo);
                }
                catch (UnsupportedEncodingException)
                {
                }
                int i;
                int u;
                int end;
                char c;
                end = userInfo.Length;
                for (i = 0, u = 0; i < end; i++)
                {
                    c = userInfo[i];
                    if (c == ';')
                    {
                        Domain = Runtime.Substring(userInfo, 0, i);
                        u = i + 1;
                    }
                    else
                    {
                        if (c == ':')
                        {
                            Password = Runtime.Substring(userInfo, i + 1);
                            break;
                        }
                    }
                }
                Username = Runtime.Substring(userInfo, u, i);
            }
            InitDefaults();
            if (Domain == null)
            {
                Domain = DefaultDomain;
            }
            if (Username == null)
            {
                Username = DefaultUsername;
            }
            if (Password == null)
            {
                Password = DefaultPassword;
            }
        }

        /// <summary>
        /// Create an <tt>NtlmPasswordAuthentication</tt> object from a
        /// domain, username, and password.
        /// </summary>
        /// <remarks>
        /// Create an <tt>NtlmPasswordAuthentication</tt> object from a
        /// domain, username, and password. Parameters that are <tt>null</tt>
        /// will be substituted with <tt>jcifs.smb.client.domain</tt>,
        /// <tt>jcifs.smb.client.username</tt>, <tt>jcifs.smb.client.password</tt>
        /// property values.
        /// </remarks>
        public NtlmPasswordAuthentication(string domain, string username, string password)
        {
            int ci;
            if (username != null)
            {
                ci = username.IndexOf('@');
                if (ci > 0)
                {
                    domain = Runtime.Substring(username, ci + 1);
                    username = Runtime.Substring(username, 0, ci);
                }
                else
                {
                    ci = username.IndexOf('\\');
                    if (ci > 0)
                    {
                        domain = Runtime.Substring(username, 0, ci);
                        username = Runtime.Substring(username, ci + 1);
                    }
                }
            }
            this.Domain = domain;
            this.Username = username;
            this.Password = password;
            InitDefaults();
            if (domain == null)
            {
                this.Domain = DefaultDomain;
            }
            if (username == null)
            {
                this.Username = DefaultUsername;
            }
            if (password == null)
            {
                this.Password = DefaultPassword;
            }
        }

        /// <summary>
        /// Create an <tt>NtlmPasswordAuthentication</tt> object with raw password
        /// hashes.
        /// </summary>
        /// <remarks>
        /// Create an <tt>NtlmPasswordAuthentication</tt> object with raw password
        /// hashes. This is used exclusively by the <tt>jcifs.http.NtlmSsp</tt>
        /// class which is in turn used by NTLM HTTP authentication functionality.
        /// </remarks>
        public NtlmPasswordAuthentication(string domain,
                                          string username,
                                          byte[] challenge,
                                          byte[] ansiHash,
                                          byte[] unicodeHash)
        {
            if (domain == null
                || username == null
                || ansiHash == null
                || unicodeHash == null)
            {
                throw new ArgumentException("External credentials cannot be null");
            }
            this.Domain = domain;
            this.Username = username;
            Password = null;
            this.Challenge = challenge;
            this.AnsiHash = ansiHash;
            this.UnicodeHash = unicodeHash;
            HashesExternal = true;
        }

        /// <summary>Returns the domain.</summary>
        /// <remarks>Returns the domain.</remarks>
        public string GetDomain()
        {
            return Domain;
        }

        /// <summary>Returns the username.</summary>
        /// <remarks>Returns the username.</remarks>
        public string GetUsername()
        {
            return Username;
        }

        /// <summary>
        /// Returns the password in plain text or <tt>null</tt> if the raw password
        /// hashes were used to construct this <tt>NtlmPasswordAuthentication</tt>
        /// object which will be the case when NTLM HTTP Authentication is
        /// used.
        /// </summary>
        /// <remarks>
        /// Returns the password in plain text or <tt>null</tt> if the raw password
        /// hashes were used to construct this <tt>NtlmPasswordAuthentication</tt>
        /// object which will be the case when NTLM HTTP Authentication is
        /// used. There is no way to retrieve a users password in plain text unless
        /// it is supplied by the user at runtime.
        /// </remarks>
        public string GetPassword()
        {
            return Password;
        }

        /// <summary>
        /// Return the domain and username in the format:
        /// <tt>domain\\username</tt>.
        /// </summary>
        /// <remarks>
        /// Return the domain and username in the format:
        /// <tt>domain\\username</tt>. This is equivalent to <tt>toString()</tt>.
        /// </remarks>
        public new string GetName()
        {
            bool d = Domain.Length > 0 && Domain.Equals("?") == false;
            return d
                    ? Domain + "\\" + Username
                    : Username;
        }

        /// <summary>
        /// Computes the 24 byte ANSI password hash given the 8 byte server challenge.
        /// </summary>
        /// <remarks>
        /// Computes the 24 byte ANSI password hash given the 8 byte server challenge.
        /// </remarks>
        public byte[] GetAnsiHash(byte[] challenge)
        {
            if (HashesExternal)
            {
                return AnsiHash;
            }
            switch (LmCompatibility)
            {
                case 0:
                case 1:
                    {
                        return GetPreNtlmResponse(Password, challenge);
                    }

                case 2:
                    {
                        return GetNtlmResponse(Password, challenge);
                    }

                case 3:
                case 4:
                case 5:
                    {
                        if (ClientChallenge == null)
                        {
                            ClientChallenge = new byte[8];
                            Random.NextBytes(ClientChallenge);
                        }
                        return GetLMv2Response(Domain,
                                               Username,
                                               Password,
                                               challenge,
                                               ClientChallenge);
                    }

                default:
                    {
                        return GetPreNtlmResponse(Password, challenge);
                    }
            }
        }

        /// <summary>
        /// Computes the 24 byte Unicode password hash given the 8 byte server challenge.
        /// </summary>
        /// <remarks>
        /// Computes the 24 byte Unicode password hash given the 8 byte server challenge.
        /// </remarks>
        public byte[] GetUnicodeHash(byte[] challenge)
        {
            if (HashesExternal)
            {
                return UnicodeHash;
            }
            switch (LmCompatibility)
            {
                case 0:
                case 1:
                case 2:
                    {
                        return GetNtlmResponse(Password, challenge);
                    }

                case 3:
                case 4:
                case 5:
                    {
                        return new byte[0];
                    }

                default:
                    {
                        return GetNtlmResponse(Password, challenge);
                    }
            }
        }

        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        public byte[] GetSigningKey(byte[] challenge)
        {
            switch (LmCompatibility)
            {
                case 0:
                case 1:
                case 2:
                    {
                        byte[] signingKey = new byte[40];
                        GetUserSessionKey(challenge, signingKey, 0);
                        Array.Copy(GetUnicodeHash(challenge), 0, signingKey, 16, 24);
                        return signingKey;
                    }

                case 3:
                case 4:
                case 5:
                    {
                        throw new SmbException(
                            "NTLMv2 requires extended security "
                            + "(jcifs.smb.client.useExtendedSecurity must be true "
                            + "if jcifs.smb.lmCompatibility >= 3)");
                    }
            }
            return null;
        }

        /// <summary>Returns the effective user session key.</summary>
        /// <remarks>Returns the effective user session key.</remarks>
        /// <param name="challenge">The server challenge.</param>
        /// <returns>
        /// A <code>byte[]</code> containing the effective user session key,
        /// used in SMB MAC signing and NTLMSSP signing and sealing.
        /// </returns>
        public byte[] GetUserSessionKey(byte[] challenge)
        {
            if (HashesExternal)
            {
                return null;
            }
            byte[] key = new byte[16];
            try
            {
                GetUserSessionKey(challenge, key, 0);
            }
            catch (Exception ex)
            {
                if (_log.Level > 0)
                {
                    Runtime.PrintStackTrace(ex, _log);
                }
            }
            return key;
        }

        /// <summary>Calculates the effective user session key.</summary>
        /// <remarks>Calculates the effective user session key.</remarks>
        /// <param name="challenge">The server challenge.</param>
        /// <param name="dest">
        /// The destination array in which the user session key will be
        /// placed.
        /// </param>
        /// <param name="offset">
        /// The offset in the destination array at which the
        /// session key will start.
        /// </param>
        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        internal void GetUserSessionKey(byte[] challenge, byte[] dest, int offset)
        {
            if (HashesExternal)
            {
                return;
            }
            try
            {
                Md4 md4 = new Md4();
                md4.Update(Runtime.GetBytesForString(Password, SmbConstants.UniEncoding));
                switch (LmCompatibility)
                {
                    case 0:
                    case 1:
                    case 2:
                        {
                            md4.Update(md4.Digest());
                            md4.Digest(dest, offset, 16);
                            break;
                        }

                    case 3:
                    case 4:
                    case 5:
                        {
                            if (ClientChallenge == null)
                            {
                                ClientChallenge = new byte[8];
                                Random.NextBytes(ClientChallenge);
                            }
                            Hmact64 hmac = new Hmact64(md4.Digest());
                            hmac.Update(Runtime.GetBytesForString(Username.ToUpper(),
                                        SmbConstants.UniEncoding));
                            hmac.Update(Runtime.GetBytesForString(Domain.ToUpper(),
                                        SmbConstants.UniEncoding));
                            byte[] ntlmv2Hash = hmac.Digest();
                            hmac = new Hmact64(ntlmv2Hash);
                            hmac.Update(challenge);
                            hmac.Update(ClientChallenge);
                            Hmact64 userKey = new Hmact64(ntlmv2Hash);
                            userKey.Update(hmac.Digest());
                            userKey.Digest(dest, offset, 16);
                            break;
                        }

                    default:
                        {
                            md4.Update(md4.Digest());
                            md4.Digest(dest, offset, 16);
                            break;
                        }
                }
            }
            catch (Exception e)
            {
                throw new SmbException(string.Empty, e);
            }
        }

        /// <summary>
        /// Compares two <tt>NtlmPasswordAuthentication</tt> objects for
        /// equality.
        /// </summary>
        /// <remarks>
        /// Compares two <tt>NtlmPasswordAuthentication</tt> objects for
        /// equality. Two <tt>NtlmPasswordAuthentication</tt> objects are equal if
        /// their caseless domain and username fields are equal and either both hashes are external and they are equal or both internally supplied passwords are equal. If one <tt>NtlmPasswordAuthentication</tt> object has external hashes (meaning negotiated via NTLM HTTP Authentication) and the other does not they will not be equal. This is technically not correct however the server 8 byte challage would be required to compute and compare the password hashes but that it not available with this method.
        /// </remarks>
        public override bool Equals(object obj)
        {
            if (obj is NtlmPasswordAuthentication)
            {
                NtlmPasswordAuthentication ntlm = (NtlmPasswordAuthentication)obj;
                if (ntlm.Domain.ToUpper().Equals(Domain.ToUpper())
                    && ntlm.Username.ToUpper().Equals(Username.ToUpper()))
                {
                    if (HashesExternal && ntlm.HashesExternal)
                    {

                        return Arrays.Equals(AnsiHash, ntlm.AnsiHash)
                               && Arrays.Equals(UnicodeHash, ntlm.UnicodeHash);
                    }
                    if (!HashesExternal && Password.Equals(ntlm.Password))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>Return the upcased username hash code.</summary>
        /// <remarks>Return the upcased username hash code.</remarks>
        public override int GetHashCode()
        {
            return GetName().ToUpper().GetHashCode();
        }

        /// <summary>
        /// Return the domain and username in the format:
        /// <tt>domain\\username</tt>.
        /// </summary>
        /// <remarks>
        /// Return the domain and username in the format:
        /// <tt>domain\\username</tt>. This is equivalent to <tt>getName()</tt>.
        /// </remarks>
        public override string ToString()
        {
            return GetName();
        }

        /// <exception cref="System.FormatException"></exception>
        /// <exception cref="UnsupportedEncodingException"></exception>
        internal static string Unescape(string str)
        {
            char ch;
            int i;
            int j;
            int state;
            int len;
            char[] @out;
            byte[] b = new byte[1];
            if (str == null)
            {
                return null;
            }
            len = str.Length;
            @out = new char[len];
            state = 0;
            for (i = j = 0; i < len; i++)
            {
                switch (state)
                {
                    case 0:
                        {
                            ch = str[i];
                            if (ch == '%')
                            {
                                state = 1;
                            }
                            else
                            {
                                @out[j++] = ch;
                            }
                            break;
                        }

                    case 1:
                        {
                            b[0] = unchecked(
                                (byte)(
                                    Convert.ToInt32(Runtime.Substring(str, i, i + 2), 16)
                                    & unchecked(0xFF)
                                )
                            );
                            @out[j++] = (Runtime.GetStringForBytes(b, 0, 1, "ASCII"))[0];
                            i++;
                            state = 0;
                            break;
                        }
                }
            }
            return new string(@out, 0, j);
        }
    }
}
