using MediaBrowser.Controller;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Api.HttpHandlers
{
    public class PersonHandler : ItemHandler
    {
        protected override BaseItem ItemToSerialize
        {
            get
            {
                return Kernel.Instance.ItemController.GetPerson(QueryString["name"]);
            }
        }
    }
}
