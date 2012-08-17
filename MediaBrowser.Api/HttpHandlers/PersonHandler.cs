using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Api.HttpHandlers
{
    public class PersonHandler : BaseJsonHandler<Person>
    {
        protected override Person GetObjectToSerialize()
        {
            return Kernel.Instance.ItemController.GetPerson(QueryString["name"]);
        }
    }
}
