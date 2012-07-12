using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Model.Entities
{
    public class Person
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public PersonType PersonType { get; set; }
    }

    public enum PersonType
    {
        Actor = 1,
        Director = 2,
        Writer = 3
    }
}
