#pragma warning disable CS0618 // Type or member is obsolete
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Data;
using Jellyfin.Data.Events.Users;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Extensions.Json;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Events.Authentication;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Dto;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;

namespace Jellyfin.Server.Implementations.Users
{
    /// <summary>
    /// Default implementation of <see cref="IUserAuthenticationManager"/>.
    /// </summary>
    public class UserAuthenticationManager(
        IDbContextFactory<JellyfinDbContext> contextFactory,
        IUserManager userManager,
        ILogger<UserAuthenticationManager> logger,
        INetworkManager networkManager,
        IEventManager eventManager,
        IReadOnlyCollection<IAuthenticationProvider>? legacyAuthProviders = null
        ) : IUserAuthenticationManager
    {
        // Dictionary<TResponseC2S, Dictionary<AuthenticationType, IAuthenticationProvider<TResponseC2S>>>
        private readonly ConcurrentDictionary<Type, PayloadHandlerInfo> _providerMap = new();
        private readonly ConcurrentDictionary<Type, object> _providersByImpType = new();

        /// <inheritdoc/>
        public async Task RegisterProviders(IEnumerable<object> providers)
        {
            var authenticationProviderTypeName = typeof(IAuthenticationProvider<>).Name;
            foreach (var provider in providers)
            {
                var providerType = provider.GetType();
                var interfaceType = providerType.GetInterface(authenticationProviderTypeName)
                    ?? throw new InvalidOperationException("Attempted to register an authentication provider that does not inherit from IAuthenticationProvider<T>.");

                var payloadHandlerInfo = _providerMap.GetOrAdd(interfaceType.GetGenericArguments()[0], new PayloadHandlerInfo() { All = new(), ByTypeFilter = new() });
                payloadHandlerInfo.All.Push(provider);

                string? authenticationType = (string?)interfaceType.GetProperty(nameof(DefaultAuthenticationProvider.AuthenticationType))!.GetValue(provider);
                if (authenticationType != null)
                {
                    payloadHandlerInfo.ByTypeFilter[authenticationType] = provider;
                }

                _providersByImpType[providerType] = provider;

                var dbContext = await contextFactory.CreateDbContextAsync().ConfigureAwait(false);
                var typeName = providerType.FullName!;
                var entryExists = false;
                await using (dbContext)
                {
                    entryExists = dbContext.AuthenticationProviderDatas.Any(provider => provider.AuthenticationProviderId == typeName);
                    if (!entryExists)
                    {
                        dbContext.AuthenticationProviderDatas.Add(new AuthenticationProviderData() { AuthenticationProviderId = typeName, IsEnabled = true });
                        await dbContext.SaveChangesAsync().ConfigureAwait(false);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the first enabled legacy authentication provider, if any.
        /// </summary>
        /// <returns>The first enabled legacy authentication provider, or else null.</returns>
        private async Task<IAuthenticationProvider?> GetLegacyAuthenticationProvider()
        {
            if (legacyAuthProviders is null)
            {
                return null;
            }

            foreach (var legacyProvider in legacyAuthProviders)
            {
                var dbContext = await contextFactory.CreateDbContextAsync().ConfigureAwait(false);
                AuthenticationProviderData? data;
                await using (dbContext.ConfigureAwait(false))
                {
                    data = dbContext.AuthenticationProviderDatas.First(dbProvider => dbProvider.AuthenticationProviderId == legacyProvider.GetType().FullName);
                }

                if (data?.IsEnabled != true)
                {
                    continue;
                }

                return legacyProvider;
            }

            return null;
        }

        /// <inheritdoc/>
        public async Task<(IAuthenticationProvider<TResponseC2S> Provider, AuthenticationResult Result)> Authenticate<TResponseC2S>(TResponseC2S authenticationData, string? remoteEndpoint, string? authenticationTypeFilter = null)
            where TResponseC2S : struct
        {
            if (authenticationData is UsernamePasswordAuthData authData) // attempt legacy fallback
            {
                var legacyProvider = await GetLegacyAuthenticationProvider().ConfigureAwait(false);

                if (legacyProvider is not null)
                {
                    ArgumentNullException.ThrowIfNull(authData.Username);
                    ArgumentNullException.ThrowIfNull(authData.Password);

                    IRequiresResolvedUser? requiresResolvedUser = legacyProvider as IRequiresResolvedUser;
                    User? resolvedUser = null;

                    if (requiresResolvedUser != null)
                    {
                        resolvedUser = userManager.GetUserByName(authData.Username);
                    }

                    try
                    {
                        var legacyAuthResult = requiresResolvedUser != null
                            ? await requiresResolvedUser.Authenticate(authData.Username, authData.Password, resolvedUser).ConfigureAwait(false)
                            : await legacyProvider.Authenticate(authData.Username, authData.Password).ConfigureAwait(false);

                        if (legacyAuthResult.Username != authData.Username)
                        {
                            logger.LogDebug("Legacy authentication provider provided updated username {1}", legacyAuthResult.Username);
                        }

                        var newestUser = userManager.GetUserByName(legacyAuthResult.Username);

                        if (newestUser is null)
                        {
                            return ((IAuthenticationProvider<TResponseC2S>)new LegacyPlaceholderAuthenticationProvider(),
                                resolvedUser is not null ? AuthenticationResult.Failure(resolvedUser) : AuthenticationResult.AnonymousFailure());
                        }

                        return ((IAuthenticationProvider<TResponseC2S>)new LegacyPlaceholderAuthenticationProvider(), AuthenticationResult.Success(newestUser));
                    }
                    catch (AuthenticationException ex)
                    {
                        logger.LogDebug(ex, "Error authenticating with legacy provider {Provider}", legacyProvider.Name);

                        return ((IAuthenticationProvider<TResponseC2S>)new LegacyPlaceholderAuthenticationProvider(),
                            resolvedUser is not null ? AuthenticationResult.Failure(resolvedUser) : AuthenticationResult.AnonymousFailure());
                    }
                }
            }

            var provider = await ResolveProvider<TResponseC2S>().ConfigureAwait(false)
                ?? throw new NotImplementedException("Attempted authentication using '" + typeof(TResponseC2S).Name + "', but found no registered provider that can handle it.");

            var authenticationResult = await provider.Authenticate(authenticationData).ConfigureAwait(false);

            if (authenticationResult.Authenticated)
            {
                var user = authenticationResult.User;
                if (user.HasPermission(PermissionKind.IsDisabled))
                {
                    logger.LogInformation(
                        "Authentication request for {UserName} has been denied because this account is currently disabled (IP: {IP}).",
                        user.Username,
                        remoteEndpoint);
                    throw new SecurityException(
                        $"The {user.Username} account is currently disabled. Please consult with your administrator.");
                }

                if (!user.HasPermission(PermissionKind.EnableRemoteAccess) &&
                    !networkManager.IsInLocalNetwork(remoteEndpoint))
                {
                    logger.LogInformation(
                        "Authentication request for {UserName} forbidden: remote access disabled and user not in local network (IP: {IP}).",
                        user.Username,
                        remoteEndpoint);
                    throw new SecurityException("Forbidden.");
                }

                if (!user.IsParentalScheduleAllowed())
                {
                    logger.LogInformation(
                        "Authentication request for {UserName} is not allowed at this time due parental restrictions (IP: {IP}).",
                        user.Username,
                        remoteEndpoint);
                    throw new SecurityException("User is not allowed access at this time.");
                }

                user.LastActivityDate = user.LastLoginDate = DateTime.UtcNow;
                user.InvalidLoginAttemptCount = 0;
                await userManager.UpdateUserAsync(user).ConfigureAwait(false);
                logger.LogInformation("Authentication request for {UserName} has succeeded.", user.Username);
            }
            else if (authenticationResult.User is not null)
            {
                await IncrementInvalidLoginAttemptCount(authenticationResult.User).ConfigureAwait(false);
                logger.LogInformation(
                    "Authentication request for user {UserName} has been denied (IP: {IP}).",
                    authenticationResult.User.Username,
                    remoteEndpoint);
            }
            else
            {
                logger.LogInformation(
                    "Authentication request with data {Data} has been denied (IP: {IP}).",
                    authenticationData,
                    remoteEndpoint);
            }

            return (provider, authenticationResult);
        }

        private async Task IncrementInvalidLoginAttemptCount(User user)
        {
            user.InvalidLoginAttemptCount++;
            int? maxInvalidLogins = user.LoginAttemptsBeforeLockout;
            if (maxInvalidLogins.HasValue && user.InvalidLoginAttemptCount >= maxInvalidLogins)
            {
                user.SetPermission(PermissionKind.IsDisabled, true);
                await eventManager.PublishAsync(new UserLockedOutEventArgs(user)).ConfigureAwait(false);
                logger.LogWarning(
                    "Disabling user {Username} due to {Attempts} unsuccessful login attempts.",
                    user.Username,
                    user.InvalidLoginAttemptCount);
            }

            await userManager.UpdateUserAsync(user).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<IAuthenticationProvider<TResponseC2S>?> ResolveProvider<TResponseC2S>(string? authenticationTypeFilter = null)
            where TResponseC2S : struct
        {
            var payloadHandlerInfo = _providerMap[typeof(TResponseC2S)];
            if (payloadHandlerInfo is null)
            {
                return null;
            }

            IEnumerable<object?> providersRaw = authenticationTypeFilter == null ? payloadHandlerInfo.All : [payloadHandlerInfo.ByTypeFilter[authenticationTypeFilter]];

            foreach (var providerRaw in providersRaw)
            {
                if (providerRaw is null)
                {
                    continue;
                }

                IAuthenticationProvider<TResponseC2S> provider = (IAuthenticationProvider<TResponseC2S>)providerRaw;

                var dbContext = await contextFactory.CreateDbContextAsync().ConfigureAwait(false);
                AuthenticationProviderData? data;
                await using (dbContext.ConfigureAwait(false))
                {
                    data = dbContext.AuthenticationProviderDatas.First(dbProvider => dbProvider.AuthenticationProviderId == provider.GetType().FullName);
                }

                if (data?.IsEnabled != true)
                {
                    continue;
                }

                return provider;
            }

            return null;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<NameIdPair>> GetAuthenticationProviders()
        {
            // TODO: revise. probably want to include legacy authentication providers too, for the time being
            // and maybe also disabled ones (depending on how API was used in the past) if this is going to be
            // used mainly for config pages and stuff, so that admins can enable/disable them through this API
            List<NameIdPair> providers = [];
            foreach (var entry in _providerMap.Values)
            {
                foreach (var providerRaw in entry.All)
                {
                    if (providerRaw is null)
                    {
                        continue;
                    }

                    var providerType = providerRaw.GetType();
                    var typeName = providerType.FullName!;

                    var dbContext = await contextFactory.CreateDbContextAsync().ConfigureAwait(false);
                    AuthenticationProviderData? data;
                    await using (dbContext.ConfigureAwait(false))
                    {
                        data = dbContext.AuthenticationProviderDatas.First(dbProvider => dbProvider.AuthenticationProviderId == typeName);
                    }

                    if (data?.IsEnabled != true)
                    {
                        continue;
                    }

                    providers.Add(new NameIdPair() { Id = typeName, Name = (string)providerType.GetProperty(nameof(DefaultAuthenticationProvider.Name))!.GetValue(providerRaw)! });
                }
            }

            return providers;
        }

        /// <inheritdoc/>
        public async Task<T?> ResolveConcrete<T>()
            where T : class
        {
            var providerType = typeof(T);
            var providerRaw = _providersByImpType[providerType];
            if (providerRaw is null)
            {
                return null;
            }

            var typeName = providerType.FullName!;
            var dbContext = await contextFactory.CreateDbContextAsync().ConfigureAwait(false);
            AuthenticationProviderData? data;
            await using (dbContext.ConfigureAwait(false))
            {
                data = dbContext.AuthenticationProviderDatas.First(dbProvider => dbProvider.AuthenticationProviderId == typeName);
            }

            if (data?.IsEnabled != true)
            {
                return null;
            }

            return providerRaw as T;
        }

        private record PayloadHandlerInfo
        {
            public required ConcurrentDictionary<string, object> ByTypeFilter { get; set; }

            public required ConcurrentStack<object> All { get; set; }
        }
    }
}
