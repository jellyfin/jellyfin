#pragma warning disable CS1591
#nullable enable

using System;
using System.IO;
using System.Linq;
using Emby.Naming.Common;

namespace Emby.Naming.Video
{
    public static class StubResolver
    {
        public static bool TryResolveFile(string path, NamingOptions options, out string? stubType)
        {
            stubType = default;

            if (path == null)
            {
                return false;
            }

            var extension = Path.GetExtension(path);

            if (!options.StubFileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            path = Path.GetFileNameWithoutExtension(path);
            var token = Path.GetExtension(path).TrimStart('.');

            foreach (var rule in options.StubTypes)
            {
                if (string.Equals(rule.Token, token, StringComparison.OrdinalIgnoreCase))
                {
                    stubType = rule.StubType;
                    return true;
                }
            }

            return true;
        }
    }
}
