using System;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Api.Helpers;

/// <inheritdoc />
public class BandwidthLimiterProviderService : IBandwidthLimiterProviderService
{
    private readonly IUserManager _userManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="BandwidthLimiterProviderService"/> class.
    /// </summary>
    /// <param name="userManager">The user manager.</param>
    public BandwidthLimiterProviderService(IUserManager userManager)
    {
        _userManager = userManager;
        _userManager.OnUserUpdated += UserManager_OnUserUpdated;
    }

    /// <inheritdoc />
    public event EventHandler<BandwidthLimitOptionEventArgs> BandwidthLimitUpdated = null!;

    private void UserManager_OnUserUpdated(object? sender, Data.Events.GenericEventArgs<Data.Entities.User> e)
    {
        OnBandwidthLimitUpdated(GetLimit(e.Argument.Id));
    }

    /// <inheritdoc />
    public BandwidthLimitOption GetLimit(Guid user)
    {
        var userById = _userManager.GetUserById(user);
        if (userById is null)
        {
            throw new InvalidOperationException($"Could not find user with id '{user}'");
        }

        return new BandwidthLimitOption()
        {
            BandwidthPerSec = userById.RemoteDownloadSpeedLimit ?? long.MaxValue,
            User = user
        };
    }

    private void OnBandwidthLimitUpdated(BandwidthLimitOption e)
    {
        BandwidthLimitUpdated?.Invoke(this, new BandwidthLimitOptionEventArgs(e));
    }
}
