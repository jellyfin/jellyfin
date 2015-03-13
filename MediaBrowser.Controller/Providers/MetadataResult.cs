namespace MediaBrowser.Controller.Providers
{
    public class MetadataResult<T>
    {
        public bool HasMetadata { get; set; }
        public T Item { get; set; }
    }
}