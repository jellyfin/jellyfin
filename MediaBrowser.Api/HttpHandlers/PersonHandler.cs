using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;

namespace MediaBrowser.Api.HttpHandlers
{
    public class PersonHandler : BaseJsonHandler
    {
        protected override object GetObjectToSerialize()
        {
            return Kernel.Instance.ItemController.GetPerson(QueryString["name"]);
        }
    }
}
