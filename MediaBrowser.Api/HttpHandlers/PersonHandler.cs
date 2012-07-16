using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Api.HttpHandlers
{
    public class PersonHandler : ItemHandler
    {
        public PersonHandler(RequestContext ctx)
            : base(ctx)
        {
        }

        protected override BaseItem ItemToSerialize
        {
            get
            {
                return Kernel.Instance.ItemController.GetPerson(QueryString["name"]);
            }
        }
    }
}
