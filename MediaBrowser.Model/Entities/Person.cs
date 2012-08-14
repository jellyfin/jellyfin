
namespace MediaBrowser.Model.Entities
{
    /// <summary>
    /// This is the full Person object that can be retrieved with all of it's data.
    /// </summary>
    public class Person : BaseEntity
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

        public override string ToString()
        {
            return Name;
        }
    }

    public enum PersonType
    {
        Other,
        Actor,
        Director,
        Writer,
        Producer
    }
}
