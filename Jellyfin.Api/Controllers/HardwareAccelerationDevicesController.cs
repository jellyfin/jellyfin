using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller.HardwareAccelerationDevices;
using MediaBrowser.Model.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Hardware acceleration devices Controller.
/// </summary>
[Authorize(Policy = Policies.RequiresElevation)]
[Tags("HardwareAccelerationDevice")]
public class HardwareAccelerationDevicesController : BaseJellyfinApiController
{
    private readonly IHardwareAccelerationDeviceManager _hardwareAccelerationDeviceManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="HardwareAccelerationDevicesController"/> class.
    /// </summary>
    /// <param name="hardwareAccelerationDeviceManager">Hardware acceleration device cache.</param>
    public HardwareAccelerationDevicesController(IHardwareAccelerationDeviceManager hardwareAccelerationDeviceManager)
    {
        _hardwareAccelerationDeviceManager = hardwareAccelerationDeviceManager ?? throw new ArgumentNullException(nameof(hardwareAccelerationDeviceManager));
    }

    /// <summary>
    /// Get hardware acceleration devices.
    /// </summary>
    /// <param name="types">Required hardware acceleration types.</param>
    /// <param name="cancellationToken">CancellationToken.</param>
    /// <response code="200">Devices retrieved.</response>
    /// <returns>An <see cref="OkResult"/> containing the list of devices.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<HardwareAccelerationDevice>>> GetDevices([FromQuery] HardwareAccelerationType[]? types, CancellationToken cancellationToken)
    {
        if (!_hardwareAccelerationDeviceManager.Devices.Any())
        {
            await _hardwareAccelerationDeviceManager.RefreshAsync(cancellationToken).ConfigureAwait(false);
        }

        var query = _hardwareAccelerationDeviceManager.Devices.AsQueryable();

        if (types is not null && types.Length > 0)
        {
            query = query.Where(x => types.Contains(x.Key));
        }

        return Ok(query.SelectMany(x => x.Value).AsEnumerable());
    }

    /// <summary>
    /// Refresh devices.
    /// </summary>
    /// <param name="types">Required hardware acceleration types.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Data refreshed.</response>
    /// <returns>An <see cref="OkResult"/> containing the list of devices.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> Refresh([FromQuery] HardwareAccelerationType[]? types, CancellationToken cancellationToken)
    {
        if (types is not null && types.Length > 0)
        {
            foreach (var type in types)
            {
                await _hardwareAccelerationDeviceManager.RefreshConcreteTypeAsync(type, cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            await _hardwareAccelerationDeviceManager.RefreshAsync(cancellationToken).ConfigureAwait(false);
        }

        return Ok();
    }
}
