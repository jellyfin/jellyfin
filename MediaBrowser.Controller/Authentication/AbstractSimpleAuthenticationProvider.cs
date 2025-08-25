using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Authentication
{
    /// <summary>
    /// An abstract authentication provider that provides convenience logic useful for most custom authentication providers.
    /// </summary>
    /// <typeparam name="TData">Type of data that you expect to receive from the user directly (TOTP code, single use authentication code, password, quick connect code).</typeparam>
    /// <typeparam name="TPersistentUserData">Type of persistent user-specific data that you want to store or access (TOTP secret, e-mail address, password hash).</typeparam>
    public abstract class AbstractSimpleAuthenticationProvider<TData, TPersistentUserData> : AbstractAuthenticationProvider<TData, NoData, NoData, TPersistentUserData>
    {
        /// <inheritdoc/>
        public override int RefreshInterval => 0;

        /// <inheritdoc/>
        public override Task<dynamic?> GetProgress(Guid attemptId)
        {
            return Task.FromResult<dynamic?>(null);
        }
    }
}
