using MediaBrowser.Controller;

namespace MediaBrowser.Api.HttpHandlers
{
    public class PersonHandler : JsonHandler
    {
        protected sealed override object ObjectToSerialize
        {
            get
            {
                return Kernel.Instance.ItemController.GetPerson(QueryString["name"]);
            }
        }
    }
}
