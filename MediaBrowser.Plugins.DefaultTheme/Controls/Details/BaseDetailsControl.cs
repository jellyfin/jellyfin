using MediaBrowser.Model.DTO;
using MediaBrowser.UI.Controls;

namespace MediaBrowser.Plugins.DefaultTheme.Controls.Details
{
    /// <summary>
    /// Class BaseDetailsControl
    /// </summary>
    public abstract class BaseDetailsControl : BaseUserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseDetailsControl" /> class.
        /// </summary>
        protected BaseDetailsControl()
        {
            DataContext = this;
        }

        /// <summary>
        /// The _item
        /// </summary>
        private DtoBaseItem _item;
        /// <summary>
        /// Gets or sets the item.
        /// </summary>
        /// <value>The item.</value>
        public DtoBaseItem Item
        {
            get { return _item; }

            set
            {
                _item = value;
                OnPropertyChanged("Item");
                OnItemChanged();
            }
        }

        /// <summary>
        /// Called when [item changed].
        /// </summary>
        protected abstract void OnItemChanged();
    }
}
