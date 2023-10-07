using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Library
{
    /// <summary>
    /// Initialize and get library root folders.
    /// </summary>
    public interface ILibraryRoot
    {
        /// <summary>
        /// Called on application start. Create root folders if they do not exist and setup static references.
        /// </summary>
        void Initialize();
    }
}
