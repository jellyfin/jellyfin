using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Mono.Nat
{
	class SocketGroup : IDisposable
	{
		Dictionary<UdpClient, List<IPAddress>> Sockets { get; }
		SemaphoreSlim SocketSendLocker { get; }

		int DefaultPort { get ; }

		public SocketGroup (Dictionary<UdpClient, List<IPAddress>> sockets, int defaultPort)
		{
			Sockets = sockets;
			DefaultPort = defaultPort;
			SocketSendLocker = new SemaphoreSlim (1, 1);
		}

		public void Dispose()
		{
			foreach (var s in Sockets)
				s.Key.Dispose();
		}

		public async Task<(IPAddress, UdpReceiveResult)> ReceiveAsync (CancellationToken token)
		{
			while (!token.IsCancellationRequested) {
				foreach (var keypair in Sockets) {
					try {
						if (keypair.Key.Available > 0) {
							var localAddress = ((IPEndPoint) keypair.Key.Client.LocalEndPoint).Address;
							var data = await keypair.Key.ReceiveAsync ();
							return (localAddress, data);
						}
					} catch (Exception) {
						// Ignore any errors
					}
				}

				await Task.Delay (10, token);
			}

            throw new TaskCanceledException();
		}

		public async Task SendAsync (byte [] buffer, IPAddress gatewayAddress, CancellationToken token)
		{
			using (await SocketSendLocker.DisposableWaitAsync (token)) {
				foreach (var keypair in Sockets) {
					try {
						if (gatewayAddress == null) {
							foreach (var defaultGateway in keypair.Value)
								await keypair.Key.SendAsync (buffer, buffer.Length, new IPEndPoint (defaultGateway, DefaultPort));
						} else {
							await keypair.Key.SendAsync (buffer, buffer.Length, new IPEndPoint (gatewayAddress, DefaultPort));
						}
					} catch (Exception) {

					}
				}
			}
		}
	}
}
