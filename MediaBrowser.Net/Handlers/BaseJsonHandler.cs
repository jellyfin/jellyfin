
namespace MediaBrowser.Net.Handlers
{
    public abstract class BaseJsonHandler : BaseHandler
    {
        public override string ContentType
        {
            get { return "application/json"; }
        }
    }
}
