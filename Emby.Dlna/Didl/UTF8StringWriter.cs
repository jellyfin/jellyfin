#pragma warning disable CS1591
#pragma warning disable CA1305

using System;
using System.IO;
using System.Text;

namespace Emby.Dlna.Didl
{
    public class UTF8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
}
