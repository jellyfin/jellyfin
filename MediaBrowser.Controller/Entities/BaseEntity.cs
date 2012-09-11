using System;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Provides a base entity for all of our types
    /// </summary>
    public abstract class BaseEntity
    {
        public string Name { get; set; }

        public Guid Id { get; set; }

        public string PrimaryImagePath { get; set; }

        public DateTime DateCreated { get; set; }

        public DateTime DateModified { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
