using System;
using MediaBrowser.Controller;

namespace MediaBrowser.Api.HttpHandlers
{
    public class UserConfigurationHandler : JsonHandler
    {
        protected override object ObjectToSerialize
        {
            get
            {
                Guid userId = Guid.Parse(QueryString["userid"]);

                return Kernel.Instance.GetUserConfiguration(userId);
            }
        }
    }
}
