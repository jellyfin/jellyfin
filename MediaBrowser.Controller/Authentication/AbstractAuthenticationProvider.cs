using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Extensions.Json;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using Microsoft.EntityFrameworkCore;
using Polly;

namespace MediaBrowser.Controller.Authentication
{
    /// <summary>
    /// An abstract authentication provider that provides convenience logic useful for most custom authentication providers.
    /// </summary>
    /// <typeparam name="TResponseC2S">The payload data that authenticates a user. This type is used as a key for signalling if an authentication provider can handle a specific type of authentication data.</typeparam>
    /// <typeparam name="TGlobalData">Global data that your authentication provider wants to store.</typeparam>
    /// <typeparam name="TUserData">User-specific data that your authentication provider wants to store.</typeparam>
    public abstract class AbstractAuthenticationProvider<TResponseC2S, TGlobalData, TUserData>(IDbContextFactory<JellyfinDbContext> contextFactory, IUserManager userManager) : IAuthenticationProvider<TResponseC2S>
        where TResponseC2S : struct
        where TUserData : struct
        where TGlobalData : struct
    {
        /// <inheritdoc/>
        public abstract string Name { get; }

        /// <summary>
        /// Gets the user manager provided to this authentication provider by DI.
        /// </summary>
        protected IUserManager UserManager => userManager;

        /// <inheritdoc/>
        public virtual string? AuthenticationType { get => null; }

        /// <summary>
        /// Gets global data for this authentication provider.
        /// </summary>
        /// <returns>The global data for this authentication provider.</returns>
        protected async Task<TGlobalData> GetGlobalData()
        {
            var dbContext = await contextFactory.CreateDbContextAsync().ConfigureAwait(false);
            var typeName = GetType().FullName;
            AuthenticationProviderData? data;
            await using (dbContext)
            {
                data = dbContext.AuthenticationProviderDatas.First(provider => provider.AuthenticationProviderId == typeName);
            }

            if (string.IsNullOrEmpty(data.Data))
            {
                return await InitialGlobalData().ConfigureAwait(false);
            }

            return JsonSerializer.Deserialize<TGlobalData>(data.Data, JsonDefaults.Options);
        }

        /// <summary>
        /// Saves global data for this authentication provider.
        /// </summary>
        /// <param name="globalData">The global data.</param>
        /// <returns>A completed task.</returns>
        protected async Task SaveGlobalData(TGlobalData globalData)
        {
            var typeName = GetType().FullName!;

            var dbContext = await contextFactory.CreateDbContextAsync().ConfigureAwait(false);
            AuthenticationProviderData? data;
            await using (dbContext)
            {
                data = dbContext.AuthenticationProviderDatas.First(provider => provider.AuthenticationProviderId == typeName);
                data.Data = JsonSerializer.Serialize(globalData, JsonDefaults.Options);
                dbContext.AuthenticationProviderDatas.Update(data);

                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets the initial global data for this authentication provider.
        /// </summary>
        /// <returns>The initial global data for this authentication provider.</returns>
        public abstract Task<TGlobalData> InitialGlobalData();

        /// <summary>
        /// Gets user data for a specific user.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>The data associated with this user.</returns>
        protected Task<TUserData?> GetUserData(User user)
        {
            ArgumentNullException.ThrowIfNull(user);

            var typeName = GetType().FullName;
            var data = user.UserAuthenticationProviderDatas.FirstOrDefault(provider => provider.AuthenticationProviderId == typeName);

            if (string.IsNullOrEmpty(data?.Data))
            {
                return Task.FromResult<TUserData?>(null);
            }

            return Task.FromResult<TUserData?>(JsonSerializer.Deserialize<TUserData>(data.Data, JsonDefaults.Options));
        }

        /// <summary>
        /// Saves user data for a specific user.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="userData">The user data.</param>
        /// <returns>A task that resolves upon completion.</returns>
        protected async Task SaveUserData(User user, TUserData userData)
        {
            ArgumentNullException.ThrowIfNull(user);

            var typeName = GetType().FullName!;
            var data = user.UserAuthenticationProviderDatas.First(provider => provider.AuthenticationProviderId == typeName);

            if (data is null)
            {
                data = new UserAuthenticationProviderData()
                {
                    AuthenticationProviderId = typeName,
                    UserId = user.Id
                };
                user.UserAuthenticationProviderDatas.Add(data);
            }

            data.Data = JsonSerializer.Serialize(userData, JsonDefaults.Options);

            await userManager.UpdateUserAsync(user).ConfigureAwait(false);
        }

        /// <summary>
        /// Saves user data for a specific user, using an updater function that uses previous user data.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="userDataFactory">The user data factory.</param>
        /// <returns>A task that resolves upon completion.</returns>
        protected async Task SaveUserData(User user, Func<TUserData?, TUserData> userDataFactory)
        {
            ArgumentNullException.ThrowIfNull(user);
            ArgumentNullException.ThrowIfNull(userDataFactory);

            var typeName = GetType().FullName!;
            var data = user.UserAuthenticationProviderDatas.First(provider => provider.AuthenticationProviderId == typeName);

            if (data is null)
            {
                data = new UserAuthenticationProviderData()
                {
                    AuthenticationProviderId = typeName,
                    UserId = user.Id
                };
                user.UserAuthenticationProviderDatas.Add(data);
            }

            data.Data = JsonSerializer.Serialize(userDataFactory(string.IsNullOrEmpty(data.Data) ? null : JsonSerializer.Deserialize<TUserData>(data.Data, JsonDefaults.Options)), JsonDefaults.Options);

            await userManager.UpdateUserAsync(user).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public abstract Task<AuthenticationResult> Authenticate(TResponseC2S authenticationData);
    }

    /// <summary>
    /// Class that can be used as a type parameter to denote the absence of data.
    /// </summary>
#pragma warning disable SA1402, SA1201 // File may only contain a single type, Elements should appear in the correct order
    public struct NoData
#pragma warning restore SA1402, SA1201 // File may only contain a single type, Elements should appear in the correct order
    {
    }
}
