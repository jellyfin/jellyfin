
namespace MediaBrowser.Model.Sync
{
    public class SyncDataResponse
    {
        public string[] ItemIdsToRemove { get; set; }

        public SyncDataResponse()
        {
            ItemIdsToRemove = new string[] { };
        }
    }
}
