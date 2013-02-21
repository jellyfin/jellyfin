using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Net;
using MediaBrowser.Plugins.DefaultTheme.DisplayPreferences;
using MediaBrowser.Plugins.DefaultTheme.Resources;
using MediaBrowser.UI;
using MediaBrowser.UI.Controls;
using MediaBrowser.UI.Pages;
using System;
using System.Windows;

namespace MediaBrowser.Plugins.DefaultTheme.Pages
{
    /// <summary>
    /// Interaction logic for ListPage.xaml
    /// </summary>
    public partial class ListPage : BaseListPage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ListPage" /> class.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        public ListPage(string itemId)
            : base(itemId)
        {
            InitializeComponent();
        }

        /// <summary>
        /// Subclasses must provide the list box that holds the items
        /// </summary>
        /// <value>The items list.</value>
        protected override ExtendedListBox ItemsList
        {
            get
            {
                return lstItems;
            }
        }

        /// <summary>
        /// If the page is using it's own image type and not honoring the DisplayPreferences setting, it should return it here
        /// </summary>
        /// <value>The type of the fixed image.</value>
        protected override ImageType? FixedImageType
        {
            get { return ImageType.Primary; }
        }

        /// <summary>
        /// Called when [loaded].
        /// </summary>
        protected override async void OnLoaded()
        {
            base.OnLoaded();

            if (Folder != null)
            {
                ShowViewButton();

                await AppResources.Instance.SetPageTitle(Folder);
            }
            else
            {
                HideViewButton();
            }
        }

        /// <summary>
        /// Called when [unloaded].
        /// </summary>
        protected override void OnUnloaded()
        {
            base.OnUnloaded();

            HideViewButton();
        }

        /// <summary>
        /// Called when [property changed].
        /// </summary>
        /// <param name="name">The name.</param>
        public override void OnPropertyChanged(string name)
        {
            base.OnPropertyChanged(name);

            if (name.Equals("CurrentItemIndex", StringComparison.OrdinalIgnoreCase))
            {
                UpdateCurrentItemIndex();
            }
        }

        /// <summary>
        /// Updates the index of the current item.
        /// </summary>
        private void UpdateCurrentItemIndex()
        {
            var index = CurrentItemIndex;

            currentItemIndex.Visibility = index == -1 ? Visibility.Collapsed : Visibility.Visible;
            currentItemIndex.Text = (CurrentItemIndex + 1).ToString();

            currentItemIndexDivider.Visibility = index == -1 ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        /// Gets called anytime the Folder gets refreshed
        /// </summary>
        protected override async void OnFolderChanged()
        {
            base.OnFolderChanged();

            var pageTitleTask = AppResources.Instance.SetPageTitle(Folder);

            ShowViewButton();

            if (Folder.IsType("Season"))
            {
                TxtName.Visibility = Visibility.Visible;
                TxtName.Text = Folder.Name;
            }
            else
            {
                TxtName.Visibility = Visibility.Collapsed;
            }

            if (!string.IsNullOrEmpty(Folder.Overview) || Folder.IsType("Series") || Folder.IsType("Season"))
            {
                sidebar.Visibility = Visibility.Collapsed;

                //RefreshSidebar();
            }
            else
            {
                sidebar.Visibility = Visibility.Collapsed;
            }

            await pageTitleTask;
        }

        /// <summary>
        /// Shows the view button.
        /// </summary>
        private void ShowViewButton()
        {
            var viewButton = AppResources.Instance.ViewButton;
            viewButton.Visibility = Visibility.Visible;
            viewButton.Click -= ViewButton_Click;
            viewButton.Click += ViewButton_Click;
        }

        /// <summary>
        /// Hides the view button.
        /// </summary>
        private void HideViewButton()
        {
            var viewButton = AppResources.Instance.ViewButton;
            viewButton.Visibility = Visibility.Collapsed;
            viewButton.Click -= ViewButton_Click;
        }

        /// <summary>
        /// Handles the Click event of the ViewButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        async void ViewButton_Click(object sender, RoutedEventArgs e)
        {
            var menu = new DisplayPreferencesMenu
            {
                FolderId = Folder.Id,
                MainPage = this
            };

            menu.ShowModal(this.GetWindow());

            try
            {
                await App.Instance.ApiClient.UpdateDisplayPreferencesAsync(App.Instance.CurrentUser.Id, Folder.Id, DisplayPreferences);
            }
            catch (HttpException)
            {
                App.Instance.ShowDefaultErrorMessage();
            }
        }

        /// <summary>
        /// Refreshes the sidebar.
        /// </summary>
        private void RefreshSidebar()
        {
            //if (Folder.BackdropCount > 0)
            //{
            //    //backdropImage.Source = App.Instance.GetBitmapImage(ApiClient.GetImageUrl(Folder.Id, Model.Entities.ImageType.Backdrop, width: 560, height: 315));
            //    backdropImage.Visibility = Visibility.Visible;
            //}
            //else
            //{
            //    backdropImage.Source = null;
            //    backdropImage.Visibility = Visibility.Collapsed;
            //}
        }

        /// <summary>
        /// Handles current item selection changes
        /// </summary>
        protected override void OnCurrentItemChanged()
        {
            base.OnCurrentItemChanged();

            // Name
            /*if (CurrentItem != null)
            {
                txtName.Visibility = CurrentItem.HasLogo ? Visibility.Collapsed : Visibility.Visible;
                currentItemLogo.Visibility = CurrentItem.HasLogo ? Visibility.Visible : Visibility.Collapsed;

                if (CurrentItem.HasLogo)
                {
                    var uri = ApiClient.GetImageUrl(CurrentItem.Id, ImageType.Logo, maxWidth: 400, maxHeight: 125);

                    Dispatcher.InvokeAsync(() => currentItemLogo.Source = App.Instance.GetBitmapImage(new Uri(uri, UriKind.Absolute)));
                }
                else
                {
                    var name = CurrentItem.Name;

                    if (!CurrentItem.IsType("Season") && CurrentItem.IndexNumber.HasValue)
                    {
                        name = CurrentItem.IndexNumber + " - " + name;
                    }

                    if (CurrentItem.IsType("Movie") && CurrentItem.ProductionYear.HasValue)
                    {
                        name += " (" + CurrentItem.ProductionYear + ")";
                    }

                    txtName.Text = name;
                }
            }
            else
            {
                txtName.Visibility = Visibility.Collapsed;
                currentItemLogo.Visibility = Visibility.Collapsed;
            }

            // PremiereDate
            if (CurrentItem != null && CurrentItem.PremiereDate.HasValue && !CurrentItem.IsType("Series"))
            {
                pnlPremiereDate.Visibility = Visibility.Visible;

                var prefix = CurrentItem.IsType("Episode") ? "Aired" : CurrentItem.IsType("Series") ? "First Aired" : "Premiered";

                txtPremiereDate.Text = string.Format("{0} {1}", prefix, CurrentItem.PremiereDate.Value.ToShortDateString());
            }
            else
            {
                pnlPremiereDate.Visibility = Visibility.Collapsed;
            }

            // Taglines
            if (CurrentItem != null && CurrentItem.Taglines != null && CurrentItem.Taglines.Length > 0)
            {
                txtTagLine.Visibility = Visibility.Visible;
                txtTagLine.Text = CurrentItem.Taglines[0];
            }
            else
            {
                txtTagLine.Visibility = Visibility.Collapsed;
            }

            // Genres
            if (CurrentItem != null && CurrentItem.Genres != null && CurrentItem.Genres.Length > 0)
            {
                txtGenres.Visibility = Visibility.Visible;

                // Try to keep them on one line by limiting to three
                txtGenres.Text = string.Join(" / ", CurrentItem.Genres.Take(3));
            }
            else
            {
                txtGenres.Visibility = Visibility.Collapsed;
            }

            // Season Number
            if (CurrentItem != null && CurrentItem.ParentIndexNumber.HasValue && CurrentItem.IsType("Episode"))
            {
                txtSeasonHeader.Visibility = Visibility.Visible;

                txtSeasonHeader.Text = string.Format("Season {0}", CurrentItem.ParentIndexNumber);
            }
            else
            {
                txtSeasonHeader.Visibility = Visibility.Collapsed;
            }

            UpdateSeriesAirTime();
            UpdateMiscellaneousFields();
            UpdateCommunityRating();
            UpdateVideoInfo();
            UpdateAudioInfo();*/
        }

        /// <summary>
        /// Updates the series air time.
        /// </summary>
        private void UpdateSeriesAirTime()
        {
            /*if (CurrentItem != null && CurrentItem.SeriesInfo != null)
            {
                var series = CurrentItem.SeriesInfo;

                txtSeriesAirTime.Visibility = Visibility.Visible;

                if (series.Status.HasValue && series.Status.Value == SeriesStatus.Ended)
                {
                    txtSeriesAirTime.Text = "Ended";
                }
                else
                {
                    string txt = "Airs";

                    if (series.AirDays.Length > 0)
                    {
                        if (series.AirDays.Length == 7)
                        {
                            txt += " Everyday";
                        }
                        else
                        {
                            txt += " " + series.AirDays[0].ToString();
                        }
                    }

                    if (CurrentItem.Studios != null && CurrentItem.Studios.Length > 0)
                    {
                        txt += " on " + CurrentItem.Studios[0].Name;
                    }

                    if (!string.IsNullOrEmpty(series.AirTime))
                    {
                        txt += " at " + series.AirTime;
                    }

                    txtSeriesAirTime.Text = txt;
                }
            }
            else
            {
                txtSeriesAirTime.Visibility = Visibility.Collapsed;
            }*/
        }

        /// <summary>
        /// Updates the miscellaneous fields.
        /// </summary>
        private void UpdateMiscellaneousFields()
        {
            /*if (CurrentItem == null)
            {
                pnlRuntime.Visibility = Visibility.Collapsed;
                pnlOfficialRating.Visibility = Visibility.Collapsed;
            }
            else
            {
                var runtimeTicks = CurrentItem.RunTimeTicks ?? 0;

                // Runtime
                if (runtimeTicks > 0)
                {
                    pnlRuntime.Visibility = Visibility.Visible;
                    txtRuntime.Text = string.Format("{0} minutes", Convert.ToInt32(TimeSpan.FromTicks(runtimeTicks).TotalMinutes));
                }
                else
                {
                    pnlRuntime.Visibility = Visibility.Collapsed;
                }

                pnlOfficialRating.Visibility = string.IsNullOrEmpty(CurrentItem.OfficialRating) ? Visibility.Collapsed : Visibility.Visible;
            }

            // Show the parent panel only if one of the children is visible
            pnlMisc.Visibility = pnlRuntime.Visibility == Visibility.Visible ||
                                           pnlOfficialRating.Visibility == Visibility.Visible
                                               ? Visibility.Visible
                                               : Visibility.Collapsed;*/
        }

        /// <summary>
        /// Updates the community rating.
        /// </summary>
        private void UpdateCommunityRating()
        {
            /*// Community Rating
            if (CurrentItem != null && CurrentItem.CommunityRating.HasValue)
            {
                pnlRating.Visibility = Visibility.Visible;
            }
            else
            {
                pnlRating.Visibility = Visibility.Collapsed;
                return;
            }

            var rating = CurrentItem.CommunityRating.Value;

            for (var i = 0; i < 10; i++)
            {
                if (rating < i - 1)
                {
                    TreeHelper.FindChild<Image>(this, "communityRatingImage" + i).SetResourceReference(Image.StyleProperty, "CommunityRatingImageEmpty");
                }
                else if (rating < i)
                {
                    TreeHelper.FindChild<Image>(this, "communityRatingImage" + i).SetResourceReference(Image.StyleProperty, "CommunityRatingImageHalf");
                }
                else
                {
                    TreeHelper.FindChild<Image>(this, "communityRatingImage" + i).SetResourceReference(Image.StyleProperty, "CommunityRatingImageFull");
                }
            }*/
        }

        /// <summary>
        /// Updates the video info.
        /// </summary>
        private void UpdateVideoInfo()
        {
            /*if (CurrentItem != null && CurrentItem.VideoInfo != null)
            {
                pnlVideoInfo.Visibility = Visibility.Visible;
            }
            else
            {
                pnlVideoInfo.Visibility = Visibility.Collapsed;
                return;
            }

            var videoInfo = CurrentItem.VideoInfo;

            if (videoInfo.VideoType == VideoType.VideoFile)
            {
                txtVideoType.Text = Path.GetExtension(CurrentItem.Path).Replace(".", string.Empty).ToLower();
            }
            else
            {
                txtVideoType.Text = videoInfo.VideoType.ToString().ToLower();
            }

            txtVideoResolution.Text = GetResolutionText(videoInfo);
            pnlVideoResolution.Visibility = string.IsNullOrEmpty(txtVideoResolution.Text) ? Visibility.Collapsed : Visibility.Visible;

            if (!string.IsNullOrEmpty(videoInfo.Codec))
            {
                pnlVideoCodec.Visibility = Visibility.Visible;
                txtVideoCodec.Text = videoInfo.Codec.ToLower();
            }
            else
            {
                pnlVideoCodec.Visibility = Visibility.Collapsed;
            }

            var audio = videoInfo.GetDefaultAudioStream();

            if (audio == null || string.IsNullOrEmpty(audio.Codec))
            {
                pnlAudioCodec.Visibility = Visibility.Collapsed;
            }
            else
            {
                pnlAudioCodec.Visibility = Visibility.Visible;
                txtAudioCodec.Text = audio.Codec.ToLower();
            }*/
        }

        /// <summary>
        /// Updates the audio info.
        /// </summary>
        private void UpdateAudioInfo()
        {
            /*if (CurrentItem != null && CurrentItem.AudioInfo != null)
            {
                pnlAudioInfo.Visibility = Visibility.Visible;
            }
            else
            {
                pnlAudioInfo.Visibility = Visibility.Collapsed;
                return;
            }

            var audioInfo = CurrentItem.AudioInfo;

            txtAudioType.Text = Path.GetExtension(CurrentItem.Path).Replace(".", string.Empty).ToLower();

            if (audioInfo.BitRate > 0)
            {
                pnlAudioBitrate.Visibility = Visibility.Visible;
                txtAudioBitrate.Text = (audioInfo.BitRate / 1000).ToString() + "kbps";
            }
            else
            {
                pnlAudioBitrate.Visibility = Visibility.Collapsed;
            }*/
        }

        /*private string GetResolutionText(VideoInfo info)
        {
            var scanType = info.ScanType ?? string.Empty;

            if (info.Height == 1080)
            {
                if (scanType.Equals("progressive", StringComparison.OrdinalIgnoreCase))
                {
                    return "1080p";
                }
                if (scanType.Equals("interlaced", StringComparison.OrdinalIgnoreCase))
                {
                    return "1080i";
                }
            }
            if (info.Height == 720)
            {
                if (scanType.Equals("progressive", StringComparison.OrdinalIgnoreCase))
                {
                    return "720p";
                }
                if (scanType.Equals("interlaced", StringComparison.OrdinalIgnoreCase))
                {
                    return "720i";
                }
            }
            if (info.Height == 480)
            {
                if (scanType.Equals("progressive", StringComparison.OrdinalIgnoreCase))
                {
                    return "480p";
                }
                if (scanType.Equals("interlaced", StringComparison.OrdinalIgnoreCase))
                {
                    return "480i";
                }
            }

            return info.Width == 0 || info.Height == 0 ? string.Empty : info.Width + "x" + info.Height;
        }*/
    }
}
