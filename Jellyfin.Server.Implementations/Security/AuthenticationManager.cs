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
        private readonly JellyfinDbProvider _dbProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationManager"/> class.
        /// </summary>
        /// <param name="dbProvider">The database provider.</param>
        public AuthenticationManager(JellyfinDbProvider dbProvider)
        {
            _dbProvider = dbProvider;
        }

        /// <inheritdoc />
        public async Task CreateApiKey(string name)
        {
            await using var dbContext = _dbProvider.CreateContext();

            dbContext.ApiKeys.Add(new ApiKey(name));

            await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<AuthenticationInfo>> GetApiKeys()
        {
            await using var dbContext = _dbProvider.CreateContext();

            return await dbContext.ApiKeys
                .AsAsyncEnumerable()
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

        /// <inheritdoc />
        public async Task DeleteApiKey(string accessToken)
        {
            await using var dbContext = _dbProvider.CreateContext();

            var key = await dbContext.ApiKeys
                .AsQueryable()
                .Where(apiKey => apiKey.AccessToken == accessToken)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);

            if (key == null)
            {
                return;
            }

            dbContext.Remove(key);

            await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}
