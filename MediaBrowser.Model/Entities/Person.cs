
namespace MediaBrowser.Model.Entities
{
    /// <summary>
    /// This is the full Person object that can be retrieved with all of it's data.
    /// </summary>
    public class Person : BaseItem
    {
        public PersonType PersonType { get; set; }
    }

    /// <summary>
    /// This is the small Person stub that is attached to BaseItems
    /// </summary>
    public class PersonInfo
    {
        public string Name { get; set; }
        public string Overview { get; set; }
        public PersonType PersonType { get; set; }
    }

    public enum PersonType
    {
        Actor,
        Director,
        Writer,
        Producer
    }
}
