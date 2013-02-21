
namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// This is the full Person object that can be retrieved with all of it's data.
    /// </summary>
    public class Person : BaseEntity
    {
    }

    /// <summary>
    /// This is the small Person stub that is attached to BaseItems
    /// </summary>
    public class PersonInfo
    {
        public string Name { get; set; }
        public string Overview { get; set; }
        public string Type { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
