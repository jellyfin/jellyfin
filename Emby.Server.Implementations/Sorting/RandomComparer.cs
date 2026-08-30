using System;
using System.Security.Cryptography;
using System.Text;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.Querying;

namespace Emby.Server.Implementations.Sorting
{
    /// <summary>
    /// Class RandomComparer.
    /// </summary>
    public class RandomComparer : IBaseItemComparer
    {
        private readonly byte[] _seedBytes;

        /// <summary>
        /// Initializes a new instance of the <see cref="RandomComparer"/> class with a per-instance seed.
        /// </summary>
        public RandomComparer()
        {
            // Per-instance random seed so different comparer instances (per request) produce different orders
            _seedBytes = Guid.NewGuid().ToByteArray();
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public ItemSortBy Type => ItemSortBy.Random;

        /// <summary>
        /// Compares the specified x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>System.Int32.</returns>
        public int Compare(BaseItem? x, BaseItem? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x is null)
            {
                return -1;
            }

            if (y is null)
            {
                return 1;
            }

            // Compute a deterministic pseudo-random key per item based on the per-instance seed and the item's id
            var hx = ComputeHashKey(x.Id);
            var hy = ComputeHashKey(y.Id);

            return hx.CompareTo(hy);
        }

        private static long ToInt64(ReadOnlySpan<byte> bytes)
        {
            // Use first 8 bytes as little-endian long
            long value = 0;
            for (int i = 0; i < 8; i++)
            {
                value |= ((long)bytes[i] & 0xff) << (8 * i);
            }

            return value;
        }

        private long ComputeHashKey(Guid id)
        {
            // SHA256(seed + id) and take first 8 bytes as signed long for comparison
            var idBytes = id.ToByteArray();
            var combined = new byte[_seedBytes.Length + idBytes.Length];
            Buffer.BlockCopy(_seedBytes, 0, combined, 0, _seedBytes.Length);
            Buffer.BlockCopy(idBytes, 0, combined, _seedBytes.Length, idBytes.Length);

            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(combined);

            return ToInt64(hash.AsSpan());
        }
    }
}
