using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;

namespace Jellyfin.Api.Tests.Controllers;

public class FailingSimilarityProvider : IItemSimilarityProvider<Audio>
{
    public string Name => "FailingProvider";

    public Task<IEnumerable<Guid>> GetSimilarItems(Audio item, int limit, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Provider failed");
    }
}
