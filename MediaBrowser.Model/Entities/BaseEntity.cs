using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaBrowser.Model.Entities
{
    /// <summary>
    /// Provides a base entity for all of our types
    /// </summary>
    public abstract class BaseEntity
    {
        public string Name { get; set; }

        public Guid Id { get; set; }

        public string PrimaryImagePath { get; set; }
    }
}
