using System.Net;

namespace SocketHttpListener.Net
{
    public delegate AuthenticationSchemes AuthenticationSchemeSelector(HttpListenerRequest httpRequest);
}
