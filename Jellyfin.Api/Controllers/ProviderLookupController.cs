using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Jellyfin.Api.Extensions;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// Controller for provider-based item existence checks.
    /// </summary>
    [Route("Library/ProviderLookup")]
    [ApiController]
    public class ProviderLookupController : ControllerBase
    {
        private readonly ILibraryManager _libraryManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProviderLookupController"/> class.
        /// </summary>
        /// <param name="libraryManager">The library manager to access the item list.</param>
        public ProviderLookupController(ILibraryManager libraryManager)
        {
            _libraryManager = libraryManager;
        }

        /// <summary>
        /// Checks if an item exists in the library by provider and provider id.
        /// </summary>
        /// <param name="provider">The provider name (e.g., 'Tmdb').</param>
        /// <param name="id">The provider id (e.g., TMDb numeric id as string).</param>
        /// <returns>True if an item exists, false otherwise.</returns>
        [HttpGet("Exists")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<bool> ExistsByProviderId(
            [FromQuery][Required] string provider,
            [FromQuery][Required] string id)
        {
            Console.WriteLine($"[ProviderLookup] Checking existence for provider: '{provider}', id: '{id}'");
            int checkedCount = 0;
            foreach (var item in _libraryManager.GetItemList(new InternalItemsQuery()))
            {
                checkedCount++;
                if (item.ProviderIds != null && item.ProviderIds.TryGetValue(provider, out var pid))
                {
                    if (pid == id)
                    {
                        Console.WriteLine($"[ProviderLookup] Found match: item '{item.Name}' (Id: {item.Id})");
                        return true;
                    }
                }
            }

            Console.WriteLine($"[ProviderLookup] No match found after scanning {checkedCount} items.");
            return false;
        }
    }
}
