using System;
using System.Net;

namespace Mono.Nat.Upnp
{
    interface IRequestMessage
    {
        HttpWebRequest Encode (out byte[] body);
    }
}
