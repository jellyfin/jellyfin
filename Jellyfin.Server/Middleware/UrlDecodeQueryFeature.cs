using System.Collections.Generic;
using System.Linq;
using System.Web;
using MediaBrowser.Common.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;

namespace Jellyfin.Server.Middleware
{
    /// <summary>
    /// Defines the <see cref="UrlDecodeQueryFeature"/>.
    /// </summary>
    public class UrlDecodeQueryFeature : IQueryFeature
    {
        private IQueryCollection? _store;

        /// <summary>
        /// Initializes a new instance of the <see cref="UrlDecodeQueryFeature"/> class.
        /// </summary>
        /// <param name="feature">The <see cref="IQueryFeature"/> instance.</param>
        public UrlDecodeQueryFeature(IQueryFeature feature)
        {
            Query = feature.Query;
        }

        /// <summary>
        /// Gets or sets a value indicating the url decoded <see cref="IQueryCollection"/>.
        /// </summary>
        public IQueryCollection Query
        {
            get
            {
                return _store ?? QueryCollection.Empty;
            }

            set
            {
                // Only interested in where the querystring is encoded which shows up as one key with nothing in the value.
                if (value.Count != 1)
                {
                    _store = value;
                    return;
                }

                // Encoded querystrings have no value, so don't process anything if a value is present.
                var kvp = value.First();
                if (!string.IsNullOrEmpty(kvp.Value))
                {
                    _store = value;
                    return;
                }

                // Unencode and re-parse querystring.
                var unencodedKey = HttpUtility.UrlDecode(kvp.Key);

                if (string.Equals(unencodedKey, kvp.Key, System.StringComparison.Ordinal))
                {
                    // Don't do anything if it's not encoded.
                    _store = value;
                    return;
                }

                var pairs = new Dictionary<string, StringValues>();
                var queryString = unencodedKey.SpanSplit('&');

                foreach (var pair in queryString)
                {
                    var item = pair.Split('=');
                    item.MoveNext();

                    var key = item.Current;
                    var val = item.MoveNext() ? item.Current : string.Empty;
                    if (key.Length == 0 && val.Length == 0)
                    {
                        // encoded is an equals.
                        pairs.Add(pair.ToString(), new StringValues(string.Empty));
                        continue;
                    }

                    pairs.Add(key.ToString(), new StringValues(val.ToString()));
                }

                _store = new QueryCollection(pairs);
            }
        }
    }
}
