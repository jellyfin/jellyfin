using MediaBrowser.Model.Dto;

namespace MediaBrowser.UI.ViewModels
{
    public class BaseItemPersonViewModel : BaseViewModel
    {
        /// <summary>
        /// The _item
        /// </summary>
        private BaseItemPerson _item;
        /// <summary>
        /// Gets or sets the item.
        /// </summary>
        /// <value>The item.</value>
        public BaseItemPerson Item
        {
            get { return _item; }

            set
            {
                _item = value;
                OnPropertyChanged("Item");
                OnPropertyChanged("Image");
            }
        }
    }
}
