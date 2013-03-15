using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaBrowser.Model.Entities
{
    /// <summary>
    /// This is a marker class that tells us that a particular item type may be physically resolved
    /// more than once within the library and we need to be sure to resolve them all to the same
    /// instance of that item.
    /// </summary>
    public interface IByReferenceItem
    {
    }
}
