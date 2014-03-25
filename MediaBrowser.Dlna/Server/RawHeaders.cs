using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Dlna.Server
{
    public class RawHeaders : Headers
    {
        public RawHeaders()
            : base(true)
        {
        }
    }
}
