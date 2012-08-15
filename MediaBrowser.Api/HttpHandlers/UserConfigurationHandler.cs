using System;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;

namespace MediaBrowser.Api.HttpHandlers
{
    public class UserConfigurationHandler : BaseJsonHandler
    {
        protected override object GetObjectToSerialize()
        {
            Guid userId = Guid.Parse(QueryString["userid"]);

            return Kernel.Instance.GetUserConfiguration(userId);
        }
    }
}
