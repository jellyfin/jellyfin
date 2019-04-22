using System;
using System.IO;
using System.Linq;
using Jellyfin.Naming.Common;

namespace Jellyfin.Naming.Video
{
    public static class StubResolver
    {
        public static StubResult ResolveFile(string path, NamingOptions options)
        {
            var result = new StubResult();
            var extension = Path.GetExtension(path) ?? string.Empty;

            if (options.StubFileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                result.IsStub = true;

                path = Path.GetFileNameWithoutExtension(path);

                var token = (Path.GetExtension(path) ?? string.Empty).TrimStart('.');

                foreach (var rule in options.StubTypes)
                {
                    if (string.Equals(rule.Token, token, StringComparison.OrdinalIgnoreCase))
                    {
                        result.StubType = rule.StubType;
                        break;
                    }
                }
            }

            return result;
        }
    }
}
