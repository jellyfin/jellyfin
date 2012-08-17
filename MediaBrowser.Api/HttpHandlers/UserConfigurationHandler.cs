using System;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Model.Configuration;

namespace MediaBrowser.Api.HttpHandlers
{
    public class UserConfigurationHandler : BaseJsonHandler<UserConfiguration>
    {
        protected override UserConfiguration GetObjectToSerialize()
        {
            Guid userId = Guid.Parse(QueryString["userid"]);

            return Kernel.Instance.GetUserConfiguration(userId);
        }
    }
}
