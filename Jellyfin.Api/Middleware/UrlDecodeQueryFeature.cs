using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;

namespace Jellyfin.Api.Middleware;

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
            var (key, stringValues) = value.First();
            if (!string.IsNullOrEmpty(stringValues))
            {
                _store = value;
                return;
            }

            if (!key.Contains('=', StringComparison.Ordinal))
            {
                _store = value;
                return;
            }

            var pairs = new Dictionary<string, StringValues>();
            foreach (var pair in key.SpanSplit('&'))
            {
                var i = pair.IndexOf('=');
                if (i == -1)
                {
                    // encoded is an equals.
                    // We use TryAdd so duplicate keys get ignored
                    pairs.TryAdd(pair.ToString(), StringValues.Empty);
                    continue;
                }

                var k = pair[..i].ToString();
                var v = pair[(i + 1)..].ToString();
                if (!pairs.TryAdd(k, new StringValues(v)))
                {
                    pairs[k] = StringValues.Concat(pairs[k], v);
                }
            }

            _store = new QueryCollection(pairs);
        }
    }
}
