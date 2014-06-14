
namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// This is just a marker interface to denote top level folders
    /// </summary>
    public interface ICollectionFolder
    {
        string CollectionType { get; }
    }
}
