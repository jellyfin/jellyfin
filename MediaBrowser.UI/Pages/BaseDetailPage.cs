using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Net;
using MediaBrowser.UI.Controller;
using MediaBrowser.UI.Playback;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediaBrowser.UI.Pages
{
    /// <summary>
    /// Provides a base class for detail pages
    /// </summary>
    public abstract class BaseDetailPage : BasePage
    {
        /// <summary>
        /// The _item id
        /// </summary>
        private string _itemId;
        /// <summary>
        /// Gets or sets the id of the item being displayed
        /// </summary>
        /// <value>The item id.</value>
        protected string ItemId
        {
            get { return _itemId; }
            private set
            {
                _itemId = value;
                OnPropertyChanged("ItemId");
            }
        }

        /// <summary>
        /// The _item
        /// </summary>
        private BaseItemDto _item;
        /// <summary>
        /// Gets or sets the item.
        /// </summary>
        /// <value>The item.</value>
        public BaseItemDto Item
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
        /// Gets a value indicating whether this instance can resume.
        /// </summary>
        /// <value><c>true</c> if this instance can resume; otherwise, <c>false</c>.</value>
        protected bool CanResume
        {
            get { return Item.CanResume; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance can queue.
        /// </summary>
        /// <value><c>true</c> if this instance can queue; otherwise, <c>false</c>.</value>
        protected bool CanQueue
        {
            get { return true; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance can play trailer.
        /// </summary>
        /// <value><c>true</c> if this instance can play trailer; otherwise, <c>false</c>.</value>
        protected bool CanPlayTrailer
        {
            get { return Item.HasTrailer; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseDetailPage" /> class.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        protected BaseDetailPage(string itemId)
            : base()
        {
            ItemId = itemId;
        }

        /// <summary>
        /// Called when [property changed].
        /// </summary>
        /// <param name="name">The name.</param>
        public async override void OnPropertyChanged(string name)
        {
            base.OnPropertyChanged(name);

            // Reload the item when the itemId changes
            if (name.Equals("ItemId"))
            {
                await ReloadItem();
            }
        }

        /// <summary>
        /// Reloads the item.
        /// </summary>
        /// <returns>Task.</returns>
        private async Task ReloadItem()
        {
            try
            {
                Item = await App.Instance.ApiClient.GetItemAsync(ItemId, App.Instance.CurrentUser.Id);
            }
            catch (HttpException)
            {
                App.Instance.ShowDefaultErrorMessage();
            }
        }

        /// <summary>
        /// Called when [item changed].
        /// </summary>
        protected virtual void OnItemChanged()
        {
            SetBackdrops(Item);
        }

        /// <summary>
        /// Plays this instance.
        /// </summary>
        public async void Play()
        {
            await UIKernel.Instance.PlaybackManager.Play(new PlayOptions
            {
                Items = new List<BaseItemDto> { Item }
            });
        }

        /// <summary>
        /// Resumes this instance.
        /// </summary>
        public async void Resume()
        {
            await UIKernel.Instance.PlaybackManager.Play(new PlayOptions
            {
                Items = new List<BaseItemDto> { Item },
                Resume = true
            });
        }

        /// <summary>
        /// Queues this instance.
        /// </summary>
        public void Queue()
        {
        }
    }
}
