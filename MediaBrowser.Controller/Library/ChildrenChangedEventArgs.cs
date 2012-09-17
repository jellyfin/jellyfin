using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Library
{
    public class ChildrenChangedEventArgs : EventArgs
    {
        public Folder Folder { get; set; }
        public List<BaseItem> ItemsAdded { get; set; }
        public IEnumerable<BaseItem> ItemsRemoved { get; set; }

        public ChildrenChangedEventArgs()
        {
            //initialize the list
            ItemsAdded = new List<BaseItem>();
        }

        /// <summary>
        /// Create the args and set the folder property
        /// </summary>
        /// <param name="folder"></param>
        public ChildrenChangedEventArgs(Folder folder)
        {
            //init the folder property
            this.Folder = folder;
            //init the list
            ItemsAdded = new List<BaseItem>();
        }
    }
}
