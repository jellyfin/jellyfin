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
using System.Diagnostics;
using System.IO;
using System.Text;
//using Windows.Storage;
//using Windows.UI.Notifications;
using SharpCifs.Util.Sharpen;

namespace SharpCifs.Util
{
    /// <summary>
    /// 0 - nothing
    /// 1 - critical [default]
    /// 2 - basic info can be logged under load
    /// 3 - almost everything
    /// N - debugging
    /// </summary>
    public class LogStream : PrintWriter
    {
        private static LogStream _inst = null;

        public int Level = 1;

        public void SetLevel(int level)
        {
            this.Level = level;
        }

        public LogStream(TextWriter other) : base(other)
        {

        }

        /// <summary>
        /// This must be called before <tt>getInstance</tt> is called or
        /// it will have no effect.
        /// </summary>
        /// <remarks>
        /// This must be called before <tt>getInstance</tt> is called or
        /// it will have no effect.
        /// </remarks>
        public static void SetInstance(TextWriter other)
        {
            //inst = new Jcifs.Util.LogStream();
            _inst = new LogStream(other);
        }

        public static LogStream GetInstance()
        {
            if (_inst == null)
            {
                SetInstance(Console.Error);
            }
            return _inst;
        }

        public override Encoding Encoding
        {
            get { throw new NotImplementedException(); }
        }
    }
}
