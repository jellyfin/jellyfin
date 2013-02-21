using MediaBrowser.Model.Dto;
using System;
using System.Linq;
using System.Windows;

namespace MediaBrowser.Plugins.DefaultTheme.Controls.Details
{
    /// <summary>
    /// Interaction logic for ItemOverview.xaml
    /// </summary>
    public partial class ItemOverview : BaseDetailsControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ItemOverview" /> class.
        /// </summary>
        public ItemOverview()
            : base()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Called when [item changed].
        /// </summary>
        protected override void OnItemChanged()
        {
            var directors = (Item.People ?? new BaseItemPerson[] { }).Where(p => string.Equals(p.Type, "director", StringComparison.OrdinalIgnoreCase)).ToList();

            if (directors.Count > 0)
            {
                PnlDirectors.Visibility = Visibility.Visible;

                Directors.Text = string.Join(" / ", directors.Take(3).Select(d => d.Name).ToArray());
                DirectorLabel.Text = directors.Count > 1 ? "directors" : "director";
            }
            else
            {
                PnlDirectors.Visibility = Visibility.Collapsed;
            }

            if (Item.Genres != null && Item.Genres.Count > 0)
            {
                PnlGenres.Visibility = Visibility.Visible;

                Genres.Text = string.Join(" / ", Item.Genres.Take(4).ToArray());
                GenreLabel.Text = Item.Genres.Count > 1 ? "genres" : "genre";
            }
            else
            {
                PnlGenres.Visibility = Visibility.Collapsed;
            }

            if (Item.Studios != null && Item.Studios.Count > 0)
            {
                PnlStudios.Visibility = Visibility.Visible;

                Studios.Text = string.Join(" / ", Item.Studios.Take(3).ToArray());
                StudiosLabel.Text = Item.Studios.Count > 1 ? "studios" : "studio";
            }
            else
            {
                PnlStudios.Visibility = Visibility.Collapsed;
            }

            if (Item.PremiereDate.HasValue)
            {
                PnlPremiereDate.Visibility = Visibility.Visible;

                PremiereDate.Text = Item.PremiereDate.Value.ToShortDateString();
            }
            else
            {
                PnlPremiereDate.Visibility = Visibility.Collapsed;
            }

            if (!string.IsNullOrEmpty(Item.Artist))
            {
                PnlArtist.Visibility = Visibility.Visible;
                Artist.Text = Item.Artist;
            }
            else
            {
                PnlArtist.Visibility = Visibility.Collapsed;
            }

            if (!string.IsNullOrEmpty(Item.Album))
            {
                PnlAlbum.Visibility = Visibility.Visible;
                Album.Text = Item.Artist;
            }
            else
            {
                PnlAlbum.Visibility = Visibility.Collapsed;
            }

            if (!string.IsNullOrEmpty(Item.AlbumArtist))
            {
                PnlAlbumArtist.Visibility = Visibility.Visible;
                AlbumArtist.Text = Item.Artist;
            }
            else
            {
                PnlAlbumArtist.Visibility = Visibility.Collapsed;
            }
    
            Overview.Text = Item.Overview;
        }
    }
}
