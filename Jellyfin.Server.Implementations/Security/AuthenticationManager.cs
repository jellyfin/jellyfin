using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data.Entities.Security;
using MediaBrowser.Controller.Security;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Server.Implementations.Security
{
    /// <inheritdoc />
    public class AuthenticationManager : IAuthenticationManager
    {
        private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationManager"/> class.
        /// </summary>
        /// <param name="dbProvider">The database provider.</param>
        public AuthenticationManager(IDbContextFactory<JellyfinDbContext> dbProvider)
        {
            _dbProvider = dbProvider;
        }

        /// <inheritdoc />
        public async Task CreateApiKey(string name)
        {
            var dbContext = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
            await using (dbContext.ConfigureAwait(false))
            {
                dbContext.ApiKeys.Add(new ApiKey(name));

                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<AuthenticationInfo>> GetApiKeys()
        {
            var dbContext = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
            await using (dbContext.ConfigureAwait(false))
            {
                return await dbContext.ApiKeys
                    .Select(key => new AuthenticationInfo
                    {
                        AppName = key.Name,
                        AccessToken = key.AccessToken,
                        DateCreated = key.DateCreated,
                        DeviceId = string.Empty,
                        DeviceName = string.Empty,
                        AppVersion = string.Empty
                    }).ToListAsync().ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task DeleteApiKey(string accessToken)
        {
            var dbContext = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
            await using (dbContext.ConfigureAwait(false))
            {
                await dbContext.ApiKeys
                    .Where(apiKey => apiKey.AccessToken == accessToken)
                    .ExecuteDeleteAsync()
                    .ConfigureAwait(false);
            }
        }
    }
}
