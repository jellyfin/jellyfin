#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Providers;

// Example plugin implementation:
// 
// public class AudioMuseAISimilarityProvider : IItemSimilarityProvider<Audio>, IItemSimilarityProvider<MusicAlbum>
// {
//     public string Name => "AudioMuse-AI";
//     
//     public async Task<IEnumerable<Guid>> GetSimilarItems(
//         Audio item,
//         int limit,
//         CancellationToken cancellationToken)
//     {
//         var response = await _httpClient.GetAsync(
//             $"http://audiomuse:8000/similar_tracks?item_id={item.Id}&n={limit}",
//             cancellationToken);
//         
//         var json = await response.Content.ReadAsAsync<AudioMuseResponse>(cancellationToken);
//         return json.SimilarItems.Select(i => i.JellyfinId).Take(limit);
//     }
// }

/// <summary>
/// Marker interface for item similarity providers.
/// </summary>
public interface IItemSimilarityProvider : IMetadataProvider
{
}

/// <summary>
/// Interface for providing similar items to a given item.
/// </summary>
/// <typeparam name="TItemType">The type of item this provider handles.</typeparam>
public interface IItemSimilarityProvider<TItemType> : IMetadataProvider<TItemType>, IItemSimilarityProvider
    where TItemType : BaseItem
{
    /// <summary>
    /// Gets a collection of similar items for the given item.
    /// </summary>
    /// <param name="item">The item to find similar items for.</param>
    /// <param name="limit">The maximum number of items to return.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing an enumerable of similar item IDs.</returns>
    Task<IEnumerable<Guid>> GetSimilarItems(
        TItemType item,
        int limit,
        CancellationToken cancellationToken);
}
