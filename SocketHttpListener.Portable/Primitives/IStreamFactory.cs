using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Model.Net;

namespace SocketHttpListener.Primitives
{
    public interface IStreamFactory
    {
        Stream CreateNetworkStream(ISocket socket, bool ownsSocket);
        Stream CreateSslStream(Stream innerStream, bool leaveInnerStreamOpen);

        Task AuthenticateSslStreamAsServer(Stream stream, ICertificate certificate);
    }
}
