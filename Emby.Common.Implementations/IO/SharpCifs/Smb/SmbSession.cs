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
using SharpCifs.Netbios;
using SharpCifs.Util.Sharpen;

namespace SharpCifs.Smb
{
	public sealed class SmbSession
	{
		private static readonly string LogonShare = Config.GetProperty("jcifs.smb.client.logonShare"
			, null);

		private static readonly int LookupRespLimit = Config.GetInt("jcifs.netbios.lookupRespLimit"
			, 3);

		private static readonly string Domain = Config.GetProperty("jcifs.smb.client.domain"
			, null);

		private static readonly string Username = Config.GetProperty("jcifs.smb.client.username"
			, null);

		private static readonly int CachePolicy = Config.GetInt("jcifs.netbios.cachePolicy"
			, 60 * 10) * 60;

		internal static NbtAddress[] DcList;

		internal static long DcListExpiration;

		internal static int DcListCounter;

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		private static NtlmChallenge Interrogate(NbtAddress addr)
		{
			UniAddress dc = new UniAddress(addr);
			SmbTransport trans = SmbTransport.GetSmbTransport(dc, 0);
			if (Username == null)
			{
				trans.Connect();
                if (SmbTransport.LogStatic.Level >= 3)
				{
                    SmbTransport.LogStatic.WriteLine("Default credentials (jcifs.smb.client.username/password)"
						 + " not specified. SMB signing may not work propertly." + "  Skipping DC interrogation."
						);
				}
			}
			else
			{
				SmbSession ssn = trans.GetSmbSession(NtlmPasswordAuthentication.Default
					);
				ssn.GetSmbTree(LogonShare, null).TreeConnect(null, null);
			}
			return new NtlmChallenge(trans.Server.EncryptionKey, dc);
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		/// <exception cref="UnknownHostException"></exception>
		public static NtlmChallenge GetChallengeForDomain()
		{
			if (Domain == null)
			{
				throw new SmbException("A domain was not specified");
			}
			lock (Domain)
			{
				long now = Runtime.CurrentTimeMillis();
				int retry = 1;
				do
				{
					if (DcListExpiration < now)
					{
						NbtAddress[] list = NbtAddress.GetAllByName(Domain, 0x1C, null, 
							null);
						DcListExpiration = now + CachePolicy * 1000L;
						if (list != null && list.Length > 0)
						{
							DcList = list;
						}
						else
						{
							DcListExpiration = now + 1000 * 60 * 15;
                            if (SmbTransport.LogStatic.Level >= 2)
							{
                                SmbTransport.LogStatic.WriteLine("Failed to retrieve DC list from WINS");
							}
						}
					}
					int max = Math.Min(DcList.Length, LookupRespLimit);
					for (int j = 0; j < max; j++)
					{
						int i = DcListCounter++ % max;
						if (DcList[i] != null)
						{
							try
							{
								return Interrogate(DcList[i]);
							}
							catch (SmbException se)
							{
                                if (SmbTransport.LogStatic.Level >= 2)
								{
                                    SmbTransport.LogStatic.WriteLine("Failed validate DC: " + DcList[i]);
                                    if (SmbTransport.LogStatic.Level > 2)
									{
                                        Runtime.PrintStackTrace(se, SmbTransport.LogStatic);
									}
								}
							}
							DcList[i] = null;
						}
					}
					DcListExpiration = 0;
				}
				while (retry-- > 0);
				DcListExpiration = now + 1000 * 60 * 15;
			}
			throw new UnknownHostException("Failed to negotiate with a suitable domain controller for "
				 + Domain);
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		/// <exception cref="UnknownHostException"></exception>
		public static byte[] GetChallenge(UniAddress dc)
		{
			return GetChallenge(dc, 0);
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		/// <exception cref="UnknownHostException"></exception>
		public static byte[] GetChallenge(UniAddress dc, int port)
		{
			SmbTransport trans = SmbTransport.GetSmbTransport(dc, port);
			trans.Connect();
			return trans.Server.EncryptionKey;
		}

		/// <summary>
		/// Authenticate arbitrary credentials represented by the
		/// <tt>NtlmPasswordAuthentication</tt> object against the domain controller
		/// specified by the <tt>UniAddress</tt> parameter.
		/// </summary>
		/// <remarks>
		/// Authenticate arbitrary credentials represented by the
		/// <tt>NtlmPasswordAuthentication</tt> object against the domain controller
		/// specified by the <tt>UniAddress</tt> parameter. If the credentials are
		/// not accepted, an <tt>SmbAuthException</tt> will be thrown. If an error
		/// occurs an <tt>SmbException</tt> will be thrown. If the credentials are
		/// valid, the method will return without throwing an exception. See the
		/// last <a href="../../../faq.html">FAQ</a> question.
		/// <p>
		/// See also the <tt>jcifs.smb.client.logonShare</tt> property.
		/// </remarks>
		/// <exception cref="SmbException"></exception>
		public static void Logon(UniAddress dc, NtlmPasswordAuthentication auth)
		{
			Logon(dc, -1, auth);
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		public static void Logon(UniAddress dc, int port, NtlmPasswordAuthentication auth
			)
		{
			SmbTree tree = SmbTransport.GetSmbTransport(dc, port).GetSmbSession(auth).GetSmbTree
				(LogonShare, null);
			if (LogonShare == null)
			{
				tree.TreeConnect(null, null);
			}
			else
			{
				Trans2FindFirst2 req = new Trans2FindFirst2("\\", "*", SmbFile.AttrDirectory);
				Trans2FindFirst2Response resp = new Trans2FindFirst2Response();
				tree.Send(req, resp);
			}
		}

		internal int ConnectionState;

		internal int Uid;

		internal List<object> Trees;

		private UniAddress _address;

		private int _port;

		private int _localPort;

		private IPAddress _localAddr;

		internal SmbTransport transport;

		internal NtlmPasswordAuthentication Auth;

		internal long Expiration;

		internal string NetbiosName;

		internal SmbSession(UniAddress address, int port, IPAddress localAddr, int localPort
			, NtlmPasswordAuthentication auth)
		{
			// Transport parameters allows trans to be removed from CONNECTIONS
			this._address = address;
			this._port = port;
			this._localAddr = localAddr;
			this._localPort = localPort;
			this.Auth = auth;
			Trees = new List<object>();
			ConnectionState = 0;
		}

		internal SmbTree GetSmbTree(string share, string service)
		{
			lock (this)
			{
				SmbTree t;
				if (share == null)
				{
					share = "IPC$";
				}
				/*for (IEnumeration e = trees.GetEnumerator(); e.MoveNext(); )
				{
					t = (SmbTree)e.Current;
					if (t.Matches(share, service))
					{
						return t;
					}
				}*/
			    foreach (var e in Trees)
			    {
                    t = (SmbTree)e;
                    if (t.Matches(share, service))
                    {
                        return t;
                    }
			    }

				t = new SmbTree(this, share, service);
				Trees.Add(t);
				return t;
			}
		}

		internal bool Matches(NtlmPasswordAuthentication auth)
		{
			return this.Auth == auth || this.Auth.Equals(auth);
		}

		internal SmbTransport Transport()
		{
			lock (this)
			{
				if (transport == null)
				{
					transport = SmbTransport.GetSmbTransport(_address, _port, _localAddr, _localPort, null
						);
				}
				return transport;
			}
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		internal void Send(ServerMessageBlock request, ServerMessageBlock response)
		{
			lock (Transport())
			{
				if (response != null)
				{
					response.Received = false;
				}
				Expiration = Runtime.CurrentTimeMillis() + SmbConstants.SoTimeout;
				SessionSetup(request, response);
				if (response != null && response.Received)
				{
					return;
				}
				if (request is SmbComTreeConnectAndX)
				{
					SmbComTreeConnectAndX tcax = (SmbComTreeConnectAndX)request;
					if (NetbiosName != null && tcax.path.EndsWith("\\IPC$"))
					{
						tcax.path = "\\\\" + NetbiosName + "\\IPC$";
					}
				}
				request.Uid = Uid;
				request.Auth = Auth;
				try
				{
					transport.Send(request, response);
				}
				catch (SmbException se)
				{
					if (request is SmbComTreeConnectAndX)
					{
						Logoff(true);
					}
					request.Digest = null;
					throw;
				}
			}
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		internal void SessionSetup(ServerMessageBlock andx, ServerMessageBlock andxResponse
			)
		{
			lock (Transport())
			{
				NtlmContext nctx = null;
				SmbException ex = null;
				SmbComSessionSetupAndX request;
				SmbComSessionSetupAndXResponse response;
				byte[] token = new byte[0];
				int state = 10;
				while (ConnectionState != 0)
				{
					if (ConnectionState == 2 || ConnectionState == 3)
					{
						// connected or disconnecting
						return;
					}
					try
					{
						Runtime.Wait(transport);
					}
					catch (Exception ie)
					{
						throw new SmbException(ie.Message, ie);
					}
				}
				ConnectionState = 1;
				// trying ...
				try
				{
					transport.Connect();
					if (transport.Log.Level >= 4)
					{
						transport.Log.WriteLine("sessionSetup: accountName=" + Auth.Username + ",primaryDomain="
							 + Auth.Domain);
					}
					Uid = 0;
					do
					{
						switch (state)
						{
							case 10:
							{
								if (Auth != NtlmPasswordAuthentication.Anonymous && transport.HasCapability(SmbConstants
									.CapExtendedSecurity))
								{
									state = 20;
									break;
								}
								request = new SmbComSessionSetupAndX(this, andx, Auth);
								response = new SmbComSessionSetupAndXResponse(andxResponse);
								if (transport.IsSignatureSetupRequired(Auth))
								{
									if (Auth.HashesExternal && NtlmPasswordAuthentication.DefaultPassword != NtlmPasswordAuthentication
										.Blank)
									{
										transport.GetSmbSession(NtlmPasswordAuthentication.Default).GetSmbTree(LogonShare
											, null).TreeConnect(null, null);
									}
									else
									{
										byte[] signingKey = Auth.GetSigningKey(transport.Server.EncryptionKey);
										request.Digest = new SigningDigest(signingKey, false);
									}
								}
								request.Auth = Auth;
								try
								{
									transport.Send(request, response);
								}
								catch (SmbAuthException sae)
								{
									throw;
								}
								catch (SmbException se)
								{
									ex = se;
								}
								if (response.IsLoggedInAsGuest && Runtime.EqualsIgnoreCase("GUEST", Auth.
									Username) == false && transport.Server.Security != SmbConstants.SecurityShare &&
									 Auth != NtlmPasswordAuthentication.Anonymous)
								{
									throw new SmbAuthException(NtStatus.NtStatusLogonFailure);
								}
								if (ex != null)
								{
									throw ex;
								}
								Uid = response.Uid;
								if (request.Digest != null)
								{
									transport.Digest = request.Digest;
								}
								ConnectionState = 2;
								state = 0;
								break;
							}

							case 20:
							{
								if (nctx == null)
								{
                                    bool doSigning = (transport.Flags2 & SmbConstants.Flags2SecuritySignatures
										) != 0;
									nctx = new NtlmContext(Auth, doSigning);
								}
                                if (SmbTransport.LogStatic.Level >= 4)
								{
                                    SmbTransport.LogStatic.WriteLine(nctx);
								}
								if (nctx.IsEstablished())
								{
									NetbiosName = nctx.GetNetbiosName();
									ConnectionState = 2;
									state = 0;
									break;
								}
								try
								{
									token = nctx.InitSecContext(token, 0, token.Length);
								}
								catch (SmbException se)
								{
									try
									{
										transport.Disconnect(true);
									}
									catch (IOException)
									{
									}
									Uid = 0;
									throw;
								}
								if (token != null)
								{
									request = new SmbComSessionSetupAndX(this, null, token);
									response = new SmbComSessionSetupAndXResponse(null);
									if (transport.IsSignatureSetupRequired(Auth))
									{
										byte[] signingKey = nctx.GetSigningKey();
										if (signingKey != null)
										{
											request.Digest = new SigningDigest(signingKey, true);
										}
									}
									request.Uid = Uid;
									Uid = 0;
									try
									{
										transport.Send(request, response);
									}
									catch (SmbAuthException sae)
									{
										throw;
									}
									catch (SmbException se)
									{
										ex = se;
										try
										{
											transport.Disconnect(true);
										}
										catch (Exception)
										{
										}
									}
									if (response.IsLoggedInAsGuest && Runtime.EqualsIgnoreCase("GUEST", Auth.
										Username) == false)
									{
										throw new SmbAuthException(NtStatus.NtStatusLogonFailure);
									}
									if (ex != null)
									{
										throw ex;
									}
									Uid = response.Uid;
									if (request.Digest != null)
									{
										transport.Digest = request.Digest;
									}
									token = response.Blob;
								}
								break;
							}

							default:
							{
								throw new SmbException("Unexpected session setup state: " + state);
							}
						}
					}
					while (state != 0);
				}
				catch (SmbException se)
				{
					Logoff(true);
					ConnectionState = 0;
					throw;
				}
				finally
				{
					Runtime.NotifyAll(transport);
				}
			}
		}

		internal void Logoff(bool inError)
		{
			lock (Transport())
			{
				if (ConnectionState != 2)
				{
					// not-connected
					return;
				}
				ConnectionState = 3;
				// disconnecting
				NetbiosName = null;

                foreach (SmbTree t in Trees)
			    {
			        t.TreeDisconnect(inError);
			    }

                if (!inError && transport.Server.Security != SmbConstants.SecurityShare)
				{
					SmbComLogoffAndX request = new SmbComLogoffAndX(null);
					request.Uid = Uid;
					try
					{
						transport.Send(request, null);
					}
					catch (SmbException)
					{
					}
					Uid = 0;
				}
				ConnectionState = 0;
				Runtime.NotifyAll(transport);
			}
		}

		public override string ToString()
		{
			return "SmbSession[accountName=" + Auth.Username + ",primaryDomain=" + Auth.Domain
				 + ",uid=" + Uid + ",connectionState=" + ConnectionState + "]";
		}
	}
}
