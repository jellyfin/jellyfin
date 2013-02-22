using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.UI.Pages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MediaBrowser.UI.ViewModels
{
    /// <summary>
    /// Class DtoBaseItemViewModel
    /// </summary>
    public class DtoBaseItemViewModel : BaseViewModel
    {
        /// <summary>
        /// The _average primary image aspect ratio
        /// </summary>
        private double _averagePrimaryImageAspectRatio;
        /// <summary>
        /// Gets the aspect ratio that should be used if displaying the primary image
        /// </summary>
        /// <value>The average primary image aspect ratio.</value>
        public double AveragePrimaryImageAspectRatio
        {
            get { return _averagePrimaryImageAspectRatio; }

            set
            {
                _averagePrimaryImageAspectRatio = value;
                OnPropertyChanged("AveragePrimaryImageAspectRatio");
            }
        }

        /// <summary>
        /// The _parent display preferences
        /// </summary>
        private DisplayPreferences _parentDisplayPreferences;
        /// <summary>
        /// Gets of sets the current DisplayPreferences
        /// </summary>
        /// <value>The parent display preferences.</value>
        public DisplayPreferences ParentDisplayPreferences
        {
            get { return _parentDisplayPreferences; }

            set
            {
                _parentDisplayPreferences = value;
                NotifyDisplayPreferencesChanged();
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
            }
        }

        /// <summary>
        /// Notifies the display preferences changed.
        /// </summary>
        public void NotifyDisplayPreferencesChanged()
        {
            OnPropertyChanged("DisplayPreferences");
        }

        /// <summary>
        /// Gets an image url that can be used to download an image from the api
        /// </summary>
        /// <param name="imageType">The type of image requested</param>
        /// <param name="imageIndex">The image index, if there are multiple. Currently only applies to backdrops. Supply null or 0 for first backdrop.</param>
        /// <returns>System.String.</returns>
        public string GetImageUrl(ImageType imageType, int? imageIndex = null)
        {
            var height = ParentDisplayPreferences.PrimaryImageHeight;

            var averageAspectRatio = BaseFolderPage.GetAspectRatio(imageType, AveragePrimaryImageAspectRatio);

            var width = height * averageAspectRatio;

            var imageOptions = new ImageOptions
            {
                ImageType = imageType,
                ImageIndex = imageIndex,
                Height = height
            };

            if (imageType == ImageType.Primary)
            {
                var currentAspectRatio = imageType == ImageType.Primary ? Item.PrimaryImageAspectRatio ?? width / height : width / height;

                // Preserve the exact AR if it deviates from the average significantly
                var preserveExactAspectRatio = Math.Abs(currentAspectRatio - averageAspectRatio) > .10;

                if (!preserveExactAspectRatio)
                {
                    imageOptions.Width = Convert.ToInt32(width);
                }
            }

            return App.Instance.ApiClient.GetImageUrl(Item, imageOptions);
        }

        /// <summary>
        /// Gets the average primary image aspect ratio.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <returns>System.Double.</returns>
        /// <exception cref="System.ArgumentNullException">items</exception>
        public static double GetAveragePrimaryImageAspectRatio(IEnumerable<BaseItemDto> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            double total = 0;
            var count = 0;

            foreach (var child in items)
            {
                var ratio = child.PrimaryImageAspectRatio ?? 0;

                if (ratio.Equals(0))
                {
                    continue;
                }

                total += ratio;
                count++;
            }

            return count == 0 ? 1 : total / count;
        }

        /// <summary>
        /// Gets the observable items.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <returns>ObservableCollection{DtoBaseItemViewModel}.</returns>
        public static ObservableCollection<DtoBaseItemViewModel> GetObservableItems(BaseItemDto[] items)
        {
            return GetObservableItems(items, GetAveragePrimaryImageAspectRatio(items));
        }
        
        /// <summary>
        /// Gets the observable items.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="averagePrimaryImageAspectRatio">The average primary image aspect ratio.</param>
        /// <param name="parentDisplayPreferences">The parent display preferences.</param>
        /// <returns>ObservableCollection{DtoBaseItemViewModel}.</returns>
        /// <exception cref="System.ArgumentNullException">items</exception>
        public static ObservableCollection<DtoBaseItemViewModel> GetObservableItems(IEnumerable<BaseItemDto> items, double averagePrimaryImageAspectRatio, DisplayPreferences parentDisplayPreferences = null)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            return new ObservableCollection<DtoBaseItemViewModel>(items.Select(i => new DtoBaseItemViewModel
            {
                Item = i,
                ParentDisplayPreferences = parentDisplayPreferences,
                AveragePrimaryImageAspectRatio = averagePrimaryImageAspectRatio
            }));
        }
    }
}
