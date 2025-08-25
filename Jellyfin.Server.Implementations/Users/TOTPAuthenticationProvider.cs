using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Controller.Authentication;

namespace Jellyfin.Server.Implementations.Users
{
    public class TOTPAuthenticationProvider : AbstractSimpleAuthenticationProvider<string, string>
    {
    }
}
