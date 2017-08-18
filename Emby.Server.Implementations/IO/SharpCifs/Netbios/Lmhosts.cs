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
using System.IO;
using SharpCifs.Smb;
using SharpCifs.Util;
using SharpCifs.Util.Sharpen;

namespace SharpCifs.Netbios
{
	public class Lmhosts
	{
		private static readonly string Filename = Config.GetProperty("jcifs.netbios.lmhosts"
			);

		private static readonly Hashtable Tab = new Hashtable();

		private static long _lastModified = 1L;

		private static int _alt;

		private static LogStream _log = LogStream.GetInstance();

		/// <summary>
		/// This is really just for
		/// <see cref="SharpCifs.UniAddress">Jcifs.UniAddress</see>
		/// . It does
		/// not throw an
		/// <see cref="UnknownHostException">Sharpen.UnknownHostException</see>
		/// because this
		/// is queried frequently and exceptions would be rather costly to
		/// throw on a regular basis here.
		/// </summary>
		public static NbtAddress GetByName(string host)
		{
			lock (typeof(Lmhosts))
			{
				return GetByName(new Name(host, 0x20, null));
			}
		}

		internal static NbtAddress GetByName(Name name)
		{
			lock (typeof(Lmhosts))
			{
				NbtAddress result = null;
				try
				{
					if (Filename != null)
					{
						FilePath f = new FilePath(Filename);
						long lm;
						if ((lm = f.LastModified()) > _lastModified)
						{
							_lastModified = lm;
							Tab.Clear();
							_alt = 0;
							
							//path -> fileStream
							//Populate(new FileReader(f));
                            Populate(new FileReader(new FileStream(f, FileMode.Open)));
						}
						result = (NbtAddress)Tab[name];
					}
				}
				catch (FileNotFoundException fnfe)
				{
					if (_log.Level > 1)
					{
						_log.WriteLine("lmhosts file: " + Filename);
						Runtime.PrintStackTrace(fnfe, _log);
					}
				}
				catch (IOException ioe)
				{
					if (_log.Level > 0)
					{
						Runtime.PrintStackTrace(ioe, _log);
					}
				}
				return result;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal static void Populate(StreamReader r)
		{
			string line;
            BufferedReader br = new BufferedReader((InputStreamReader)r);
			while ((line = br.ReadLine()) != null)
			{
				line = line.ToUpper().Trim();
				if (line.Length == 0)
				{
				}
				else
				{
					if (line[0] == '#')
					{
						if (line.StartsWith("#INCLUDE "))
						{
							line = Runtime.Substring(line, line.IndexOf('\\'));
							string url = "smb:" + line.Replace('\\', '/');
							if (_alt > 0)
							{
								try
								{
									Populate(new InputStreamReader(new SmbFileInputStream(url)));
								}
								catch (IOException ioe)
								{
									_log.WriteLine("lmhosts URL: " + url);
									Runtime.PrintStackTrace(ioe, _log);
									continue;
								}
								_alt--;
								while ((line = br.ReadLine()) != null)
								{
									line = line.ToUpper().Trim();
									if (line.StartsWith("#END_ALTERNATE"))
									{
										break;
									}
								}
							}
							else
							{
								Populate(new InputStreamReader(new SmbFileInputStream(url)));
							}
						}
						else
						{
							if (line.StartsWith("#BEGIN_ALTERNATE"))
							{
								_alt++;
							}
							else
							{
								if (line.StartsWith("#END_ALTERNATE") && _alt > 0)
								{
									_alt--;
									throw new IOException("no lmhosts alternate includes loaded");
								}
							}
						}
					}
					else
					{
						if (char.IsDigit(line[0]))
						{
							char[] data = line.ToCharArray();
							int ip;
							int i;
							int j;
							Name name;
							NbtAddress addr;
							char c;
							c = '.';
							ip = i = 0;
							for (; i < data.Length && c == '.'; i++)
							{
								int b = unchecked(0x00);
								for (; i < data.Length && (c = data[i]) >= 48 && c <= 57; i++)
								{
									b = b * 10 + c - '0';
								}
								ip = (ip << 8) + b;
							}
							while (i < data.Length && char.IsWhiteSpace(data[i]))
							{
								i++;
							}
							j = i;
							while (j < data.Length && char.IsWhiteSpace(data[j]) == false)
							{
								j++;
							}
							name = new Name(Runtime.Substring(line, i, j), unchecked(0x20), null
								);
							addr = new NbtAddress(name, ip, false, NbtAddress.BNode, false, false, true, true
								, NbtAddress.UnknownMacAddress);
							Tab.Put(name, addr);
						}
					}
				}
			}
		}
	}
}
