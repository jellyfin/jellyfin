using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Networking.HappyEyeballs;

/// <summary>
/// Connect callback for <see cref="SocketsHttpHandler"/> using the runtime's native Happy Eyeballs (<see cref="ConnectAlgorithm.Parallel"/>).
/// </summary>
public static class HttpClientExtension
{
    /// <summary>
    /// Implements the httpclient callback method.
    /// </summary>
    /// <param name="context">The <see cref="SocketsHttpConnectionContext"/> instance.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> instance.</param>
    /// <returns>The http steam.</returns>
    public static async ValueTask<Stream> OnConnect(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        var socket = await ConnectAsync(context.DnsEndPoint, cancellationToken).ConfigureAwait(false);
        try
        {
            // Turn off Nagle's algorithm since it degrades performance in most HttpClient scenarios.
            socket.NoDelay = true;

            // The stream takes ownership of the socket, closing it when it's disposed.
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    private static async Task<Socket> ConnectAsync(DnsEndPoint endPoint, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var args = new ConnectEventArgs { RemoteEndPoint = endPoint };
        await using (cancellationToken.UnsafeRegister(static state => Socket.CancelConnectAsync((SocketAsyncEventArgs)state!), args).ConfigureAwait(false))
        {
            if (!Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, args, ConnectAlgorithm.Parallel))
            {
                args.SetResult();
            }

            try
            {
                return await args.Task.ConfigureAwait(false);
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.OperationAborted && cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(null, ex, cancellationToken);
            }
        }
    }

    private sealed class ConnectEventArgs : SocketAsyncEventArgs
    {
        private readonly TaskCompletionSource<Socket> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<Socket> Task => _tcs.Task;

        public void SetResult()
        {
            if (SocketError == SocketError.Success && ConnectSocket is not null)
            {
                _tcs.TrySetResult(ConnectSocket);
            }
            else
            {
                _tcs.TrySetException(ConnectByNameError ?? new SocketException((int)SocketError));
            }
        }

        protected override void OnCompleted(SocketAsyncEventArgs e) => SetResult();
    }
}
