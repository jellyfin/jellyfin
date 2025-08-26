using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;

namespace MediaBrowser.Controller.Authentication
{
    /// <summary>
    /// An abstract authentication provider that provides convenience logic useful for most custom authentication providers.
    /// </summary>
    /// <typeparam name="TData">Type of data that you expect to receive from the user directly (TOTP code, single use authentication code, password, quick connect code).</typeparam>
    /// <typeparam name="TPrivateAttemptData">Type of data that you want to store over the lifetime of this attempt (single use authentication code).</typeparam>
    /// <typeparam name="TIntermediateAttemptData">Type of data that you want to report to the user between starting and ending this attempt (Quick connect code, progress indication). You should NOT put secrets in here.</typeparam>
    /// <typeparam name="TPersistentUserData">Type of persistent user-specific data that you want to store or access (TOTP secret, e-mail address, password hash).</typeparam>
    /// <remarks>You can use <see cref="NoData"/> for any type parameter to indicate the absence of data.</remarks>
    public abstract class AbstractAuthenticationProvider<TData, TPrivateAttemptData, TIntermediateAttemptData, TPersistentUserData> : IAuthenticationProvider
    {
        private readonly ConcurrentDictionary<Guid, TPrivateAttemptData> _privateAttemptDataMap = new();
        private readonly ConcurrentDictionary<Guid, TIntermediateAttemptData> _publicAttemptDataMap = new();

        /// <inheritdoc/>
        public abstract string Name { get; }

        /// <inheritdoc/>
        public bool IsEnabled => true;

        /// <summary>
        /// Gets a value indicating the interval in which this authentication method would like to provide its public data
        /// via <see cref="GetPublicAttemptData(Guid)"/>, in seconds. Returning 0 disables automatic refreshing.
        /// </summary>
        /// <remarks>
        /// Defaults to 0 (no refreshing).
        /// Public data is mostly useful for time-based 
        /// user intervention, such as Quick Connect, e-mail magic link sign-in or TOTP 2fa.
        /// </remarks>
        public virtual int RefreshInterval { get => 0; }

        /// <inheritdoc/>
        [Obsolete("Deprecated, do not override.")]
        public Task<ProviderAuthenticationResult> Authenticate(string username, string password)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        [Obsolete("Deprecated, do not override.")]
        public Task ChangePassword(User user, string newPassword)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        [Obsolete("Deprecated, do not override.")]
        public bool HasPassword(User user)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<User?> Authenticate(Guid attemptId, User? user, dynamic? authenticationData)
        {
            if (!typeof(TPrivateAttemptData).IsAssignableFrom(typeof(NoData)))
            {
                _privateAttemptDataMap[attemptId] = await InitialPrivateAttemptDataForUser(user).ConfigureAwait(false);
            }

            if (!typeof(TPublicAttemptData).IsAssignableFrom(typeof(NoData)))
            {
                _publicAttemptDataMap[attemptId] = await InitialPublicAttemptDataForUser(user).ConfigureAwait(false);
            }

            
        }

        /// <summary>
        /// Creates initial private attempt data when a given user is trying to authenticate.
        /// </summary>
        /// <param name="user">A user trying to authenticate.</param>
        /// <returns>Initial private attempt data for the given user.</returns>
        /// <remarks>This function should not have any externally observable side effects.</remarks>
        protected abstract Task<TPrivateAttemptData> InitialPrivateAttemptDataForUser(User? user);

        /// <summary>
        /// Creates initial public attempt data when a given user is trying to authenticate.
        /// </summary>
        /// <param name="user">A user trying to authenticate.</param>
        /// <returns>Initial public attempt data for the given user.</returns>
        /// <remarks>This function should not have any externally observable side effects.</remarks>
        protected abstract Task<TPublicAttemptData> InitialPublicAttemptDataForUser(User? user);

        protected abstract Task<User?> Authenticate(User? user, TData authenticationData);

        /// <inheritdoc/>
        public Task<dynamic?> GetPublicAttemptData(Guid attemptId)
        {
            return Task.FromResult<dynamic?>(_publicAttemptDataMap[attemptId]);
        }

        ///// <summary>
        ///// Same as <see cref="GetPublicAttemptData(Guid)"/>, but returns a <typeparamref name="TPublicAttemptData"/>.
        ///// </summary>
        ///// <param name="attemptId">ID of the attempt.</param>
        ///// <returns>The <typeparamref name="TPublicAttemptData"/> associated with this attempt.</returns>
        // public Task<TPublicAttemptData> GetPublicAttemptDataConcrete(Guid attemptId)
        // {
        //    return Task.FromResult(_publicAttemptDataMap[attemptId]);
        // }
    }

    /// <summary>
    /// Class that can be used as a type parameter to denote the absence of data.
    /// </summary>
#pragma warning disable SA1402 // File may only contain a single type
    public class NoData
#pragma warning restore SA1402 // File may only contain a single type
    {
    }
}
