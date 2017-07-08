// SmbFileExtensions.cs implementation by J. Arturo <webmaster at komodosoft dot net>
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
using System.Threading.Tasks;
using SharpCifs.Util.Sharpen;

namespace SharpCifs.Smb
{
    public static class SmbFileExtensions
    {
        /// <summary>
        /// Get file's creation date converted to local timezone
        /// </summary>
        /// <param name="smbFile"></param>
        /// <returns></returns>
        public static DateTime GetLocalCreateTime(this SmbFile smbFile)
        {
            return TimeZoneInfo.ConvertTime(Extensions.CreateDateFromUTC(smbFile.CreateTime()),
                TimeZoneInfo.Local);
        }

        /// <summary>
        /// Get file's last modified date converted to local timezone
        /// </summary>
        /// <param name="smbFile"></param>
        /// <returns></returns>
        public static DateTime GetLocalLastModified(this SmbFile smbFile)
        {
            return TimeZoneInfo.ConvertTime(Extensions.CreateDateFromUTC(smbFile.LastModified()),
                TimeZoneInfo.Local);
        }


        /// <summary>
        /// List files async
        /// </summary>
        /// <param name="smbFile"></param>
        /// <returns></returns>
        public static Task<SmbFile[]> ListFilesAsync(this SmbFile smbFile)
        {
            return Task.Run(() => smbFile.ListFiles());
        }

        /// <summary>
        /// List files async
        /// </summary>
        /// <param name="smbFile"></param>
        /// <param name="wildcard"></param>
        /// <returns></returns>
        public static Task<SmbFile[]> ListFilesAsync(this SmbFile smbFile, string wildcard)
        {
            return Task.Run(() => smbFile.ListFiles(wildcard));
        }

        /// <summary>
        /// List files async
        /// </summary>
        /// <param name="smbFile"></param>
        /// <returns></returns>
        public static Task<string[]> ListAsync(this SmbFile smbFile)
        {
            return Task.Run(() => smbFile.List());
        }

        /// <summary>
        /// MkDir async method
        /// </summary>
        /// <param name="smbFile"></param>
        /// <returns></returns>
        public static Task MkDirAsync(this SmbFile smbFile)
        {
            return Task.Run(() => smbFile.Mkdir());
        }


        /// <summary>
        /// Delete file async
        /// </summary>
        /// <param name="smbFile"></param>
        /// <returns></returns>
        public static Task DeleteAsync(this SmbFile smbFile)
        {
            return Task.Run(() => smbFile.Delete());
        }

        /// <summary>
        /// Rename file async
        /// </summary>
        /// <param name="smbFile"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        public static Task RenameToAsync(this SmbFile smbFile, SmbFile destination)
        {
            return Task.Run(() => smbFile.RenameTo(destination));
        }

        /// <summary>
        /// Get input stream async
        /// </summary>
        /// <param name="smbFile"></param>
        /// <returns></returns>
        public static Task<InputStream> GetInputStreamAsync(this SmbFile smbFile)
        {
            return Task.Run(() => smbFile.GetInputStream());
        }


        /// <summary>
        /// Get output stream async
        /// </summary>
        /// <param name="smbFile"></param>
        /// <param name="append"></param>
        /// <returns></returns>
        public static Task<OutputStream> GetOutputStreamAsync(this SmbFile smbFile, bool append = false)
        {
            return Task.Run(() => new SmbFileOutputStream(smbFile, append) as OutputStream);
        }
    }
}
