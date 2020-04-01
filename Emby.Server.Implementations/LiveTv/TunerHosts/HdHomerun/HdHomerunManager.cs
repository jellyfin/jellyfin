#pragma warning disable CS1591

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.LiveTv;

namespace Emby.Server.Implementations.LiveTv.TunerHosts.HdHomerun
{
    public interface IHdHomerunChannelCommands
    {
        IEnumerable<(string, string)> GetCommands();
    }

    public class LegacyHdHomerunChannelCommands : IHdHomerunChannelCommands
    {
        private string _channel;
        private string _program;
        public LegacyHdHomerunChannelCommands(string url)
        {
            // parse url for channel and program
            var regExp = new Regex(@"\/ch(\d+)-?(\d*)");
            var match = regExp.Match(url);
            if (match.Success)
            {
                _channel = match.Groups[1].Value;
                _program = match.Groups[2].Value;
            }
        }

        public IEnumerable<(string, string)> GetCommands()
        {
            if (!string.IsNullOrEmpty(_channel))
            {
                yield return ("channel", _channel);
            }

            if (!string.IsNullOrEmpty(_program))
            {
                yield return ("program", _program);
            }
        }
    }

    public class HdHomerunChannelCommands : IHdHomerunChannelCommands
    {
        private string _channel;
        private string _profile;

        public HdHomerunChannelCommands(string channel, string profile)
        {
            _channel = channel;
            _profile = profile;
        }

        public IEnumerable<(string, string)> GetCommands()
        {
            if (!string.IsNullOrEmpty(_channel))
            {
                if (!string.IsNullOrEmpty(_profile)
                    && !string.Equals(_profile, "native", StringComparison.OrdinalIgnoreCase))
                {
                    yield return ("vchannel", $"{_channel} transcode={_profile}");
                }
                else
                {
                    yield return ("vchannel", _channel);
                }
            }
        }
    }

    public class HdHomerunManager : IDisposable
    {
        public const int HdHomeRunPort = 65001;

        // Message constants
        private const byte GetSetName = 3;
        private const byte GetSetValue = 4;
        private const byte GetSetLockkey = 21;
        private const ushort GetSetRequest = 4;
        private const ushort GetSetReply = 5;

        private uint? _lockkey = null;
        private int _activeTuner = -1;
        private IPEndPoint _remoteEndPoint;

        private TcpClient _tcpClient;

        public void Dispose()
        {
            using (var socket = _tcpClient)
            {
                if (socket != null)
                {
                    _tcpClient = null;

                    StopStreaming(socket).GetAwaiter().GetResult();
                }
            }
        }

        public async Task<bool> CheckTunerAvailability(IPAddress remoteIp, int tuner, CancellationToken cancellationToken)
        {
            using (var client = new TcpClient(new IPEndPoint(remoteIp, HdHomeRunPort)))
            using (var stream = client.GetStream())
            {
                return await CheckTunerAvailability(stream, tuner, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task<bool> CheckTunerAvailability(NetworkStream stream, int tuner, CancellationToken cancellationToken)
        {
            var lockkeyMsg = CreateGetMessage(tuner, "lockkey");
            await stream.WriteAsync(lockkeyMsg, 0, lockkeyMsg.Length, cancellationToken).ConfigureAwait(false);

            byte[] buffer = ArrayPool<byte>.Shared.Rent(8192);
            try
            {
                int receivedBytes = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);

                ParseReturnMessage(buffer, receivedBytes, out string returnVal);

                return string.Equals(returnVal, "none", StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public async Task StartStreaming(IPAddress remoteIp, IPAddress localIp, int localPort, IHdHomerunChannelCommands commands, int numTuners, CancellationToken cancellationToken)
        {
            _remoteEndPoint = new IPEndPoint(remoteIp, HdHomeRunPort);

            _tcpClient = new TcpClient(_remoteEndPoint);

            if (!_lockkey.HasValue)
            {
                var rand = new Random();
                _lockkey = (uint)rand.Next();
            }

            var lockKeyValue = _lockkey.Value;
            var stream = _tcpClient.GetStream();

            byte[] buffer = ArrayPool<byte>.Shared.Rent(8192);
            try
            {
                for (int i = 0; i < numTuners; ++i)
                {
                    if (!await CheckTunerAvailability(stream, i, cancellationToken).ConfigureAwait(false))
                    {
                        continue;
                    }

                    _activeTuner = i;
                    var lockKeyString = string.Format("{0:d}", lockKeyValue);
                    var lockkeyMsg = CreateSetMessage(i, "lockkey", lockKeyString, null);
                    await stream.WriteAsync(lockkeyMsg, 0, lockkeyMsg.Length, cancellationToken).ConfigureAwait(false);
                    int receivedBytes = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);

                    // parse response to make sure it worked
                    if (!ParseReturnMessage(buffer, receivedBytes, out _))
                    {
                        continue;
                    }

                    var commandList = commands.GetCommands();
                    foreach (var command in commandList)
                    {
                        var channelMsg = CreateSetMessage(i, command.Item1, command.Item2, lockKeyValue);
                        await stream.WriteAsync(channelMsg, 0, channelMsg.Length, cancellationToken).ConfigureAwait(false);
                        receivedBytes = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);

                        // parse response to make sure it worked
                        if (!ParseReturnMessage(buffer, receivedBytes, out _))
                        {
                            await ReleaseLockkey(_tcpClient, lockKeyValue).ConfigureAwait(false);
                            continue;
                        }
                    }

                    var targetValue = string.Format("rtp://{0}:{1}", localIp, localPort);
                    var targetMsg = CreateSetMessage(i, "target", targetValue, lockKeyValue);

                    await stream.WriteAsync(targetMsg, 0, targetMsg.Length, cancellationToken).ConfigureAwait(false);
                    receivedBytes = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);

                    // parse response to make sure it worked
                    if (!ParseReturnMessage(buffer, receivedBytes, out _))
                    {
                        await ReleaseLockkey(_tcpClient, lockKeyValue).ConfigureAwait(false);
                        continue;
                    }

                    return;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }

            _activeTuner = -1;
            throw new LiveTvConflictException();
        }

        public async Task ChangeChannel(IHdHomerunChannelCommands commands, CancellationToken cancellationToken)
        {
            if (!_lockkey.HasValue)
            {
                return;
            }

            using (var tcpClient = new TcpClient(_remoteEndPoint))
            using (var stream = tcpClient.GetStream())
            {
                var commandList = commands.GetCommands();
                byte[] buffer = ArrayPool<byte>.Shared.Rent(8192);
                try
                {
                    foreach (var command in commandList)
                    {
                        var channelMsg = CreateSetMessage(_activeTuner, command.Item1, command.Item2, _lockkey);
                        await stream.WriteAsync(channelMsg, 0, channelMsg.Length, cancellationToken).ConfigureAwait(false);
                        int receivedBytes = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);

                        // parse response to make sure it worked
                        if (!ParseReturnMessage(buffer, receivedBytes, out _))
                        {
                            return;
                        }
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
        }

        public Task StopStreaming(TcpClient client)
        {
            var lockKey = _lockkey;

            if (!lockKey.HasValue)
            {
                return Task.CompletedTask;
            }

            return ReleaseLockkey(client, lockKey.Value);
        }

        private async Task ReleaseLockkey(TcpClient client, uint lockKeyValue)
        {
            var stream = client.GetStream();

            var releaseTarget = CreateSetMessage(_activeTuner, "target", "none", lockKeyValue);
            await stream.WriteAsync(releaseTarget, 0, releaseTarget.Length).ConfigureAwait(false);

            var buffer = ArrayPool<byte>.Shared.Rent(8192);
            try
            {
                await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                var releaseKeyMsg = CreateSetMessage(_activeTuner, "lockkey", "none", lockKeyValue);
                _lockkey = null;
                await stream.WriteAsync(releaseKeyMsg, 0, releaseKeyMsg.Length).ConfigureAwait(false);
                await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private static byte[] CreateGetMessage(int tuner, string name)
        {
            var byteName = Encoding.UTF8.GetBytes(string.Format(CultureInfo.InvariantCulture, "/tuner{0}/{1}\0", tuner, name));
            int messageLength = byteName.Length + 10; // 4 bytes for header + 4 bytes for crc + 2 bytes for tag name and length

            var message = new byte[messageLength];

            int offset = InsertHeaderAndName(byteName, messageLength, message);

            bool flipEndian = BitConverter.IsLittleEndian;

            // calculate crc and insert at the end of the message
            var crcBytes = BitConverter.GetBytes(HdHomerunCrc.GetCrc32(message, messageLength - 4));
            if (flipEndian)
            {
                Array.Reverse(crcBytes);
            }

            Buffer.BlockCopy(crcBytes, 0, message, offset, 4);

            return message;
        }

        private static byte[] CreateSetMessage(int tuner, string name, string value, uint? lockkey)
        {
            var byteName = Encoding.UTF8.GetBytes(string.Format(CultureInfo.InvariantCulture, "/tuner{0}/{1}\0", tuner, name));
            var byteValue = Encoding.UTF8.GetBytes(string.Format(CultureInfo.InvariantCulture, "{0}\0", value));

            int messageLength = byteName.Length + byteValue.Length + 12;
            if (lockkey.HasValue)
            {
                messageLength += 6;
            }

            var message = new byte[messageLength];

            int offset = InsertHeaderAndName(byteName, messageLength, message);

            bool flipEndian = BitConverter.IsLittleEndian;

            message[offset++] = GetSetValue;
            message[offset++] = Convert.ToByte(byteValue.Length);
            Buffer.BlockCopy(byteValue, 0, message, offset, byteValue.Length);
            offset += byteValue.Length;
            if (lockkey.HasValue)
            {
                message[offset++] = GetSetLockkey;
                message[offset++] = 4;
                var lockKeyBytes = BitConverter.GetBytes(lockkey.Value);
                if (flipEndian)
                {
                    Array.Reverse(lockKeyBytes);
                }

                Buffer.BlockCopy(lockKeyBytes, 0, message, offset, 4);
                offset += 4;
            }

            // calculate crc and insert at the end of the message
            var crcBytes = BitConverter.GetBytes(HdHomerunCrc.GetCrc32(message, messageLength - 4));
            if (flipEndian)
            {
                Array.Reverse(crcBytes);
            }

            Buffer.BlockCopy(crcBytes, 0, message, offset, 4);

            return message;
        }

        private static int InsertHeaderAndName(byte[] byteName, int messageLength, byte[] message)
        {
            // check to see if we need to flip endiannes
            bool flipEndian = BitConverter.IsLittleEndian;
            int offset = 0;

            // create header bytes
            var getSetBytes = BitConverter.GetBytes(GetSetRequest);
            var msgLenBytes = BitConverter.GetBytes((ushort)(messageLength - 8)); // Subtrace 4 bytes for header and 4 bytes for crc

            if (flipEndian)
            {
                Array.Reverse(getSetBytes);
                Array.Reverse(msgLenBytes);
            }

            // insert header bytes into message
            Buffer.BlockCopy(getSetBytes, 0, message, offset, 2);
            offset += 2;
            Buffer.BlockCopy(msgLenBytes, 0, message, offset, 2);
            offset += 2;

            // insert tag name and length
            message[offset++] = GetSetName;
            message[offset++] = Convert.ToByte(byteName.Length);

            // insert name string
            Buffer.BlockCopy(byteName, 0, message, offset, byteName.Length);
            offset += byteName.Length;

            return offset;
        }

        private static bool ParseReturnMessage(byte[] buf, int numBytes, out string returnVal)
        {
            returnVal = string.Empty;

            if (numBytes < 4)
            {
                return false;
            }

            var flipEndian = BitConverter.IsLittleEndian;
            int offset = 0;
            byte[] msgTypeBytes = new byte[2];
            Buffer.BlockCopy(buf, offset, msgTypeBytes, 0, msgTypeBytes.Length);

            if (flipEndian)
            {
                Array.Reverse(msgTypeBytes);
            }

            var msgType = BitConverter.ToUInt16(msgTypeBytes, 0);
            offset += 2;

            if (msgType != GetSetReply)
            {
                return false;
            }

            byte[] msgLengthBytes = new byte[2];
            Buffer.BlockCopy(buf, offset, msgLengthBytes, 0, msgLengthBytes.Length);
            if (flipEndian)
            {
                Array.Reverse(msgLengthBytes);
            }

            var msgLength = BitConverter.ToUInt16(msgLengthBytes, 0);
            offset += 2;

            if (numBytes < msgLength + 8)
            {
                return false;
            }

            offset++; // Name Tag

            var nameLength = buf[offset++];

            // skip the name field to get to value for return
            offset += nameLength;

            offset++; // Value Tag

            var valueLength = buf[offset++];

            returnVal = Encoding.UTF8.GetString(buf, offset, valueLength - 1); // remove null terminator
            return true;
        }

        private static class HdHomerunCrc
        {
            private static uint[] crc_table = {
            0x00000000, 0x77073096, 0xee0e612c, 0x990951ba,
            0x076dc419, 0x706af48f, 0xe963a535, 0x9e6495a3,
            0x0edb8832, 0x79dcb8a4, 0xe0d5e91e, 0x97d2d988,
            0x09b64c2b, 0x7eb17cbd, 0xe7b82d07, 0x90bf1d91,
            0x1db71064, 0x6ab020f2, 0xf3b97148, 0x84be41de,
            0x1adad47d, 0x6ddde4eb, 0xf4d4b551, 0x83d385c7,
            0x136c9856, 0x646ba8c0, 0xfd62f97a, 0x8a65c9ec,
            0x14015c4f, 0x63066cd9, 0xfa0f3d63, 0x8d080df5,
            0x3b6e20c8, 0x4c69105e, 0xd56041e4, 0xa2677172,
            0x3c03e4d1, 0x4b04d447, 0xd20d85fd, 0xa50ab56b,
            0x35b5a8fa, 0x42b2986c, 0xdbbbc9d6, 0xacbcf940,
            0x32d86ce3, 0x45df5c75, 0xdcd60dcf, 0xabd13d59,
            0x26d930ac, 0x51de003a, 0xc8d75180, 0xbfd06116,
            0x21b4f4b5, 0x56b3c423, 0xcfba9599, 0xb8bda50f,
            0x2802b89e, 0x5f058808, 0xc60cd9b2, 0xb10be924,
            0x2f6f7c87, 0x58684c11, 0xc1611dab, 0xb6662d3d,
            0x76dc4190, 0x01db7106, 0x98d220bc, 0xefd5102a,
            0x71b18589, 0x06b6b51f, 0x9fbfe4a5, 0xe8b8d433,
            0x7807c9a2, 0x0f00f934, 0x9609a88e, 0xe10e9818,
            0x7f6a0dbb, 0x086d3d2d, 0x91646c97, 0xe6635c01,
            0x6b6b51f4, 0x1c6c6162, 0x856530d8, 0xf262004e,
            0x6c0695ed, 0x1b01a57b, 0x8208f4c1, 0xf50fc457,
            0x65b0d9c6, 0x12b7e950, 0x8bbeb8ea, 0xfcb9887c,
            0x62dd1ddf, 0x15da2d49, 0x8cd37cf3, 0xfbd44c65,
            0x4db26158, 0x3ab551ce, 0xa3bc0074, 0xd4bb30e2,
            0x4adfa541, 0x3dd895d7, 0xa4d1c46d, 0xd3d6f4fb,
            0x4369e96a, 0x346ed9fc, 0xad678846, 0xda60b8d0,
            0x44042d73, 0x33031de5, 0xaa0a4c5f, 0xdd0d7cc9,
            0x5005713c, 0x270241aa, 0xbe0b1010, 0xc90c2086,
            0x5768b525, 0x206f85b3, 0xb966d409, 0xce61e49f,
            0x5edef90e, 0x29d9c998, 0xb0d09822, 0xc7d7a8b4,
            0x59b33d17, 0x2eb40d81, 0xb7bd5c3b, 0xc0ba6cad,
            0xedb88320, 0x9abfb3b6, 0x03b6e20c, 0x74b1d29a,
            0xead54739, 0x9dd277af, 0x04db2615, 0x73dc1683,
            0xe3630b12, 0x94643b84, 0x0d6d6a3e, 0x7a6a5aa8,
            0xe40ecf0b, 0x9309ff9d, 0x0a00ae27, 0x7d079eb1,
            0xf00f9344, 0x8708a3d2, 0x1e01f268, 0x6906c2fe,
            0xf762575d, 0x806567cb, 0x196c3671, 0x6e6b06e7,
            0xfed41b76, 0x89d32be0, 0x10da7a5a, 0x67dd4acc,
            0xf9b9df6f, 0x8ebeeff9, 0x17b7be43, 0x60b08ed5,
            0xd6d6a3e8, 0xa1d1937e, 0x38d8c2c4, 0x4fdff252,
            0xd1bb67f1, 0xa6bc5767, 0x3fb506dd, 0x48b2364b,
            0xd80d2bda, 0xaf0a1b4c, 0x36034af6, 0x41047a60,
            0xdf60efc3, 0xa867df55, 0x316e8eef, 0x4669be79,
            0xcb61b38c, 0xbc66831a, 0x256fd2a0, 0x5268e236,
            0xcc0c7795, 0xbb0b4703, 0x220216b9, 0x5505262f,
            0xc5ba3bbe, 0xb2bd0b28, 0x2bb45a92, 0x5cb36a04,
            0xc2d7ffa7, 0xb5d0cf31, 0x2cd99e8b, 0x5bdeae1d,
            0x9b64c2b0, 0xec63f226, 0x756aa39c, 0x026d930a,
            0x9c0906a9, 0xeb0e363f, 0x72076785, 0x05005713,
            0x95bf4a82, 0xe2b87a14, 0x7bb12bae, 0x0cb61b38,
            0x92d28e9b, 0xe5d5be0d, 0x7cdcefb7, 0x0bdbdf21,
            0x86d3d2d4, 0xf1d4e242, 0x68ddb3f8, 0x1fda836e,
            0x81be16cd, 0xf6b9265b, 0x6fb077e1, 0x18b74777,
            0x88085ae6, 0xff0f6a70, 0x66063bca, 0x11010b5c,
            0x8f659eff, 0xf862ae69, 0x616bffd3, 0x166ccf45,
            0xa00ae278, 0xd70dd2ee, 0x4e048354, 0x3903b3c2,
            0xa7672661, 0xd06016f7, 0x4969474d, 0x3e6e77db,
            0xaed16a4a, 0xd9d65adc, 0x40df0b66, 0x37d83bf0,
            0xa9bcae53, 0xdebb9ec5, 0x47b2cf7f, 0x30b5ffe9,
            0xbdbdf21c, 0xcabac28a, 0x53b39330, 0x24b4a3a6,
            0xbad03605, 0xcdd70693, 0x54de5729, 0x23d967bf,
            0xb3667a2e, 0xc4614ab8, 0x5d681b02, 0x2a6f2b94,
            0xb40bbe37, 0xc30c8ea1, 0x5a05df1b, 0x2d02ef8d };

            public static uint GetCrc32(byte[] bytes, int numBytes)
            {
                var hash = 0xffffffff;
                for (var i = 0; i < numBytes; i++)
                {
                    hash = (hash >> 8) ^ crc_table[(hash ^ bytes[i]) & 0xff];
                }

                var tmp = ~hash & 0xffffffff;
                var b0 = tmp & 0xff;
                var b1 = (tmp >> 8) & 0xff;
                var b2 = (tmp >> 16) & 0xff;
                var b3 = (tmp >> 24) & 0xff;
                return (b0 << 24) | (b1 << 16) | (b2 << 8) | b3;
            }
        }
    }
}
