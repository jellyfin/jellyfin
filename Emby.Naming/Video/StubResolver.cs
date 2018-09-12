using Emby.Naming.Common;
using System;
using System.IO;
using System.Linq;

namespace Emby.Naming.Video
{
    public class StubResolver
    {
        private readonly NamingOptions _options;

        public StubResolver(NamingOptions options)
        {
            _options = options;
        }

        public StubResult ResolveFile(string path)
        {
            var result = new StubResult();
            var extension = Path.GetExtension(path) ?? string.Empty;
            
            if (_options.StubFileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                result.IsStub = true;

                path = Path.GetFileNameWithoutExtension(path);

                var token = (Path.GetExtension(path) ?? string.Empty).TrimStart('.');

                foreach (var rule in _options.StubTypes)
                {
                    if (string.Equals(rule.Token, token, StringComparison.OrdinalIgnoreCase))
                    {
                        result.StubType = rule.StubType;
                        result.Tokens.Add(token);
                        break;
                    }
                }
            }

            return result;
        }
    }
}
