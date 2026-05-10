using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;

namespace Jellyfin.Api.Tests.Controllers;

public class MockSimilarityProvider : IItemSimilarityProvider<Audio>
{
    private readonly IEnumerable<Guid> _results;

    public MockSimilarityProvider(IEnumerable<Guid> results)
    {
        _results = results;
    }

    public string Name => "MockProvider";

    public Task<IEnumerable<Guid>> GetSimilarItems(
        Audio item,
        int limit,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_results.Take(limit));
    }
}
