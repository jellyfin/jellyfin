using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using Jellyfin.MediaEncoding.Keyframes.Matroska;
using Xunit;

namespace Jellyfin.MediaEncoding.Keyframes.Tests.Matroska
{
    public class MatroskaKeyframeExtractorTests
    {
        [Fact]
        public void GetKeyframeData_SkipsCuePointsWithCueReference()
        {
            var mkvBytes = BuildMinimalMkv(
                timestampScale: 1_000_000,
                cuePoints:
                [
                    (0UL, false),
                    (42UL, true),
                    (83UL, true),
                    (5000UL, false),
                    (5042UL, true),
                    (10000UL, false)
                ]);

            var tempFile = Path.Combine(Path.GetTempPath(), $"jellyfin-mkv-test-{Guid.NewGuid():N}.mkv");
            try
            {
                File.WriteAllBytes(tempFile, mkvBytes);

                var result = MatroskaKeyframeExtractor.GetKeyframeData(tempFile);

                Assert.Equal(
                    new long[] { 0, 50_000_000, 100_000_000 },
                    result.KeyframeTicks);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        private static byte[] BuildMinimalMkv(ulong timestampScale, IReadOnlyList<(ulong CueTime, bool HasCueReference)> cuePoints)
        {
            var infoPayload = Element(0x2AD7B1, UIntPayload(timestampScale));
            var info = Element(0x1549A966, infoPayload);

            var trackEntryPayload = Combine(
                Element(0xD7, UIntPayload(1)),
                Element(0x83, UIntPayload(1)));
            var tracks = Element(0x1654AE6B, Element(0xAE, trackEntryPayload));

            var cueElements = new List<byte[]>(cuePoints.Count);
            foreach (var (cueTime, hasCueReference) in cuePoints)
            {
                var trackPositionsChildren = new List<byte[]>
                {
                    Element(0xF7, UIntPayload(1)),
                    Element(0xF1, UIntPayload(0))
                };
                if (hasCueReference)
                {
                    trackPositionsChildren.Add(Element(0xDB, Element(0x96, UIntPayload(0))));
                }

                var cuePointPayload = Combine(
                    Element(0xB3, UIntPayload(cueTime)),
                    Element(0xB7, Combine(trackPositionsChildren.ToArray())));
                cueElements.Add(Element(0xBB, cuePointPayload));
            }

            var cues = Element(0x1C53BB6B, Combine(cueElements.ToArray()));

            // Build a placeholder SeekHead first to know its exact byte length, then compute actual offsets relative to Segment payload.
            var dummySeekHead = BuildSeekHead(0, 0, 0);
            var infoPos = (ulong)dummySeekHead.Length;
            var tracksPos = infoPos + (ulong)info.Length;
            var cuesPos = tracksPos + (ulong)tracks.Length;
            var seekHead = BuildSeekHead(infoPos, tracksPos, cuesPos);

            // Ensure SeekHead size didn't change; pad or rebuild if needed.
            if (seekHead.Length != dummySeekHead.Length)
            {
                infoPos = (ulong)seekHead.Length;
                tracksPos = infoPos + (ulong)info.Length;
                cuesPos = tracksPos + (ulong)tracks.Length;
                seekHead = BuildSeekHead(infoPos, tracksPos, cuesPos);
            }

            var segmentPayload = Combine(seekHead, info, tracks, cues);
            var segment = Element(0x18538067, segmentPayload);

            var ebmlHeader = Element(0x1A45DFA3, Element(0x4282, System.Text.Encoding.ASCII.GetBytes("matroska")));
            return Combine(ebmlHeader, segment);
        }

        private static byte[] BuildSeekHead(ulong infoPos, ulong tracksPos, ulong cuesPos)
        {
            return Element(
                0x114D9B74,
                Combine(
                    BuildSeek(0x1549A966, infoPos),
                    BuildSeek(0x1654AE6B, tracksPos),
                    BuildSeek(0x1C53BB6B, cuesPos)));
        }

        private static byte[] BuildSeek(uint targetId, ulong position)
        {
            var idBytes = new byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(idBytes, targetId);
            return Element(
                0x4DBB,
                Combine(
                    Element(0x53AB, idBytes),
                    Element(0x53AC, UIntPayload(position))));
        }

        private static byte[] Element(ulong id, byte[] payload)
        {
            var idBytes = EncodeId(id);
            var sizeBytes = EncodeSize(payload.Length);
            return Combine(idBytes, sizeBytes, payload);
        }

        private static byte[] EncodeId(ulong id)
        {
            if (id <= 0xFF)
            {
                return [(byte)id];
            }

            if (id <= 0xFFFF)
            {
                return [(byte)(id >> 8), (byte)id];
            }

            if (id <= 0xFFFFFF)
            {
                return [(byte)(id >> 16), (byte)(id >> 8), (byte)id];
            }

            return [(byte)(id >> 24), (byte)(id >> 16), (byte)(id >> 8), (byte)id];
        }

        private static byte[] EncodeSize(int size)
        {
            if (size < 0x7F)
            {
                return [(byte)(0x80 | size)];
            }

            if (size < 0x3FFF)
            {
                return [(byte)(0x40 | (size >> 8)), (byte)size];
            }

            throw new ArgumentOutOfRangeException(nameof(size));
        }

        private static byte[] UIntPayload(ulong value)
        {
            if (value <= 0xFF)
            {
                return [(byte)value];
            }

            if (value <= 0xFFFF)
            {
                return [(byte)(value >> 8), (byte)value];
            }

            if (value <= 0xFFFFFF)
            {
                return [(byte)(value >> 16), (byte)(value >> 8), (byte)value];
            }

            if (value <= 0xFFFFFFFF)
            {
                return [(byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value];
            }

            var bytes = new byte[8];
            BinaryPrimitives.WriteUInt64BigEndian(bytes, value);
            return bytes;
        }

        private static byte[] Combine(params byte[][] arrays)
        {
            var total = 0;
            foreach (var arr in arrays)
            {
                total += arr.Length;
            }

            var result = new byte[total];
            var offset = 0;
            foreach (var arr in arrays)
            {
                Buffer.BlockCopy(arr, 0, result, offset, arr.Length);
                offset += arr.Length;
            }

            return result;
        }
    }
}
