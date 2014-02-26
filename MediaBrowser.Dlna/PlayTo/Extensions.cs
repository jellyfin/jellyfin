using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MediaBrowser.Dlna.PlayTo
{
    public static class Extensions
    {
        public static Task<int> ReceiveAsync(this Socket socket, byte[] buffer, int offset, int size)
        {
            var tcs = new TaskCompletionSource<int>(socket);
            var remoteip = new IPEndPoint(IPAddress.Any, 0);
            var endpoint = (EndPoint)remoteip;

            socket.BeginReceiveFrom(buffer, offset, size, SocketFlags.None, ref endpoint, iar =>
            {
                var result = (TaskCompletionSource<int>)iar.AsyncState;
                var iarSocket = (Socket)result.Task.AsyncState;

                try
                {
                    result.TrySetResult(iarSocket.EndReceive(iar));
                }
                catch (Exception exc)
                {
                    result.TrySetException(exc);
                }
            }, tcs);

            return tcs.Task;
        }
        
        public static string GetValue(this XElement container, XName name)
        {
            var node = container.Element(name);

            return node == null ? null : node.Value;
        }

        public static string GetDescendantValue(this XElement container, XName name)
        {
            var node = container.Descendants(name)
                .FirstOrDefault();

            return node == null ? null : node.Value;
        }
    }
}
