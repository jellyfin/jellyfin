namespace MediaBrowser.Controller
{
    public interface IResourceFileManager
    {
        string GetResourcePath(string basePath, string virtualPath);
    }
}
