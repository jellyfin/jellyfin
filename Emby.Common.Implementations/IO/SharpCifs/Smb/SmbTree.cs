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
	class SmbTree
	{
		private static int _treeConnCounter;

		internal int ConnectionState;

		internal int Tid;

		internal string Share;

		internal string Service = "?????";

		internal string Service0;

		internal SmbSession Session;

		internal bool InDfs;

		internal bool InDomainDfs;

		internal int TreeNum;

		internal SmbTree(SmbSession session, string share, string service)
		{
			// used by SmbFile.isOpen
			this.Session = session;
			this.Share = share.ToUpper();
			if (service != null && service.StartsWith("??") == false)
			{
				this.Service = service;
			}
			Service0 = this.Service;
			ConnectionState = 0;
		}

		internal virtual bool Matches(string share, string service)
		{
			return Runtime.EqualsIgnoreCase(this.Share, share) && (service == null ||
				 service.StartsWith("??") || Runtime.EqualsIgnoreCase(this.Service, service
				));
		}

		public override bool Equals(object obj)
		{
			if (obj is SmbTree)
			{
				SmbTree tree = (SmbTree)obj;
				return Matches(tree.Share, tree.Service);
			}
			return false;
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		internal virtual void Send(ServerMessageBlock request, ServerMessageBlock response
			)
		{
			lock (Session.Transport())
			{
				if (response != null)
				{
					response.Received = false;
				}
				TreeConnect(request, response);
				if (request == null || (response != null && response.Received))
				{
					return;
				}
				if (Service.Equals("A:") == false)
				{
					switch (request.Command)
					{
						case ServerMessageBlock.SmbComOpenAndx:
						case ServerMessageBlock.SmbComNtCreateAndx:
						case ServerMessageBlock.SmbComReadAndx:
						case ServerMessageBlock.SmbComWriteAndx:
						case ServerMessageBlock.SmbComClose:
						case ServerMessageBlock.SmbComTreeDisconnect:
						{
							break;
						}

						case ServerMessageBlock.SmbComTransaction:
						case ServerMessageBlock.SmbComTransaction2:
						{
							switch (((SmbComTransaction)request).SubCommand & unchecked(0xFF))
							{
								case SmbComTransaction.NetShareEnum:
								case SmbComTransaction.NetServerEnum2:
								case SmbComTransaction.NetServerEnum3:
								case SmbComTransaction.TransPeekNamedPipe:
								case SmbComTransaction.TransWaitNamedPipe:
								case SmbComTransaction.TransCallNamedPipe:
								case SmbComTransaction.TransTransactNamedPipe:
								case SmbComTransaction.Trans2GetDfsReferral:
								{
									break;
								}

								default:
								{
									throw new SmbException("Invalid operation for " + Service + " service");
								}
							}
							break;
						}

						default:
						{
							throw new SmbException("Invalid operation for " + Service + " service" + request);
						}
					}
				}
				request.Tid = Tid;
				if (InDfs && !Service.Equals("IPC") && !string.IsNullOrEmpty(request.Path))
				{
                    request.Flags2 = SmbConstants.Flags2ResolvePathsInDfs;
					request.Path = '\\' + Session.Transport().TconHostName + '\\' + Share + request.Path;
				}
				try
				{
					Session.Send(request, response);
				}
				catch (SmbException se)
				{
					if (se.GetNtStatus() == NtStatus.NtStatusNetworkNameDeleted)
					{
						TreeDisconnect(true);
					}
					throw;
				}
			}
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		internal virtual void TreeConnect(ServerMessageBlock andx, ServerMessageBlock andxResponse
			)
		{
			lock (Session.Transport())
			{
				string unc;
				while (ConnectionState != 0)
				{
					if (ConnectionState == 2 || ConnectionState == 3)
					{
						// connected or disconnecting
						return;
					}
					try
					{
						Runtime.Wait(Session.transport);
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
					Session.transport.Connect();
					unc = "\\\\" + Session.transport.TconHostName + '\\' + Share;
					Service = Service0;
					if (Session.transport.Log.Level >= 4)
					{
						Session.transport.Log.WriteLine("treeConnect: unc=" + unc + ",service=" + Service
							);
					}
					SmbComTreeConnectAndXResponse response = new SmbComTreeConnectAndXResponse(andxResponse
						);
					SmbComTreeConnectAndX request = new SmbComTreeConnectAndX(Session, unc, Service, 
						andx);
					Session.Send(request, response);
					Tid = response.Tid;
					Service = response.Service;
					InDfs = response.ShareIsInDfs;
					TreeNum = _treeConnCounter++;
					ConnectionState = 2;
				}
				catch (SmbException se)
				{
					// connected
					TreeDisconnect(true);
					ConnectionState = 0;
					throw;
				}
			}
		}

		internal virtual void TreeDisconnect(bool inError)
		{
			lock (Session.Transport())
			{
				if (ConnectionState != 2)
				{
					// not-connected
					return;
				}
				ConnectionState = 3;
				// disconnecting
				if (!inError && Tid != 0)
				{
					try
					{
						Send(new SmbComTreeDisconnect(), null);
					}
					catch (SmbException se)
					{
						if (Session.transport.Log.Level > 1)
						{
							Runtime.PrintStackTrace(se, Session.transport.Log);
						}
					}
				}
				InDfs = false;
				InDomainDfs = false;
				ConnectionState = 0;
				Runtime.NotifyAll(Session.transport);
			}
		}

		public override string ToString()
		{
			return "SmbTree[share=" + Share + ",service=" + Service + ",tid=" + Tid + ",inDfs="
				 + InDfs + ",inDomainDfs=" + InDomainDfs + ",connectionState=" + ConnectionState
				 + "]";
		}
	}
}
