using MediaBrowser.Model.Dto;
using MediaBrowser.UI;
using MediaBrowser.UI.Controller;
using MediaBrowser.UI.ViewModels;
using System.Collections.ObjectModel;

namespace MediaBrowser.Plugins.DefaultTheme.Controls.Details
{
    /// <summary>
    /// Interaction logic for ItemPerformers.xaml
    /// </summary>
    public partial class ItemPerformers : BaseDetailsControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ItemPerformers" /> class.
        /// </summary>
        public ItemPerformers()
        {
            InitializeComponent();
        }

        /// <summary>
        /// The _itemsResult
        /// </summary>
        private ItemsResult _itemsResult;
        /// <summary>
        /// Gets or sets the children of the Folder being displayed
        /// </summary>
        /// <value>The children.</value>
        public ItemsResult ItemsResult
        {
            get { return _itemsResult; }

            private set
            {
                _itemsResult = value;
                OnPropertyChanged("ItemsResult");

                Items = DtoBaseItemViewModel.GetObservableItems(ItemsResult.Items);
            }
        }

        /// <summary>
        /// The _display children
        /// </summary>
        private ObservableCollection<DtoBaseItemViewModel> _items;
        /// <summary>
        /// Gets the actual children that should be displayed.
        /// Subclasses should bind to this, not ItemsResult.
        /// </summary>
        /// <value>The display children.</value>
        public ObservableCollection<DtoBaseItemViewModel> Items
        {
            get { return _items; }

            private set
            {
                _items = value;
                //lstItems.ItemsSource = value;
                OnPropertyChanged("Items");
            }
        }
        
        /// <summary>
        /// Called when [item changed].
        /// </summary>
        protected override async void OnItemChanged()
        {
            ItemsResult = await UIKernel.Instance.ApiClient.GetAllPeopleAsync(App.Instance.CurrentUser.Id, itemId: Item.Id);
        }
    }
}
