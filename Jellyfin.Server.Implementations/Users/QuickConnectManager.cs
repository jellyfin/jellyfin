using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.QuickConnect;
using Microsoft.EntityFrameworkCore;

namespace Emby.Server.Implementations.QuickConnect
{
    /// <summary>
    /// Quick connect implementation.
    /// </summary>
    public class QuickConnectManager(
        IUserManager userManager,
        IDbContextFactory<JellyfinDbContext> contextFactory)
        : AbstractExternallyTriggeredAuthenticationProvider<NoData, NoData, QuickConnectResult>(contextFactory, userManager)
    {
        /// <inheritdoc/>
        public override string? AuthenticationType => "QuickConnect";

        /// <inheritdoc/>
        public override string Name => "Quick Connect";

        private int CodeLength { get => 6; }

        /// <inheritdoc/>
        public override Task<NoData> InitialGlobalData()
        {
            return Task.FromResult(default(NoData));
        }

        /// <inheritdoc/>
        public override Task<NoData> InitialUserData()
        {
            return Task.FromResult(default(NoData));
        }

        /// <inheritdoc/>
        protected override Task<AuthenticationResult> AuthenticateAttempt(QuickConnectResult attemptData)
        {
            if (attemptData is null || !attemptData.UserId.HasValue)
            {
                return Task.FromResult(AuthenticationResult.AnonymousFailure());
            }

            var foundUser = UserManager.GetUserById(attemptData.UserId.Value);

            return Task.FromResult(foundUser is not null ? AuthenticationResult.Success(foundUser) : AuthenticationResult.AnonymousFailure());
        }

        /// <inheritdoc/>
        public async override Task<string> GenerateUpdateKey(QuickConnectResult data)
        {
            var settings = await GetGlobalData().ConfigureAwait(false);
            Span<byte> raw = stackalloc byte[4];

            int min = (int)Math.Pow(10, CodeLength - 1);
            int max = (int)Math.Pow(10, CodeLength);

            uint scale = uint.MaxValue;
            while (scale == uint.MaxValue)
            {
                RandomNumberGenerator.Fill(raw);
                scale = BitConverter.ToUInt32(raw);
            }

            int code = (int)(min + ((max - min) * (scale / (double)uint.MaxValue)));
            return code.ToString(CultureInfo.InvariantCulture);
        }
    }
}
