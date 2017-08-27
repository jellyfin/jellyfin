
namespace MediaBrowser.Model.Social
{
    public interface ISharingRepository
    {
        void CreateShare(SocialShareInfo info);
        void DeleteShare(string id);
        SocialShareInfo GetShareInfo(string id);
    }
}
