using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Net;
using MediaBrowser.UI;
using MediaBrowser.UI.Controller;
using MediaBrowser.UI.Controls;
using MediaBrowser.UI.Playback;
using MediaBrowser.UI.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Plugins.DefaultTheme.Controls.Details
{
    /// <summary>
    /// Interaction logic for ItemSpecialFeatures.xaml
    /// </summary>
    public partial class ItemSpecialFeatures : BaseDetailsControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ItemSpecialFeatures" /> class.
        /// </summary>
        public ItemSpecialFeatures()
        {
            InitializeComponent();

            lstItems.ItemInvoked += lstItems_ItemInvoked;
        }

        /// <summary>
        /// LSTs the items_ item invoked.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        void lstItems_ItemInvoked(object sender, ItemEventArgs<object> e)
        {
            var viewModel = (SpecialFeatureViewModel) e.Argument;

            UIKernel.Instance.PlaybackManager.Play(new PlayOptions
            {
                Items = new List<BaseItemDto> { viewModel.Item }
            });
        }

        /// <summary>
        /// Called when [item changed].
        /// </summary>
        protected override async void OnItemChanged()
        {
            BaseItemDto[] result;

            try
            {
                result = await UIKernel.Instance.ApiClient.GetSpecialFeaturesAsync(App.Instance.CurrentUser.Id, Item.Id);
            }
            catch (HttpException)
            {
                App.Instance.ShowDefaultErrorMessage();

                return;
            }

            var resultItems = result ?? new BaseItemDto[] { };

            const int height = 297;
            var aspectRatio = DtoBaseItemViewModel.GetAveragePrimaryImageAspectRatio(resultItems);

            var width = aspectRatio == 0 ? 528 : height * aspectRatio;

            lstItems.ItemsSource = resultItems.Select(i => new SpecialFeatureViewModel
            {
                Item = i,
                ImageHeight = height,
                ImageWidth = width

            }).ToList();
        }
    }
}
