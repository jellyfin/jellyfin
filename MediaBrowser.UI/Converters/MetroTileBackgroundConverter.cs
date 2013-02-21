using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MediaBrowser.UI.Converters
{
    /// <summary>
    /// Generates a random metro-friendly background color
    /// </summary>
    public class MetroTileBackgroundConverter : IValueConverter
    {
        private static readonly Brush[] TileColors = new Brush[] {
                new SolidColorBrush(Color.FromRgb((byte)111,(byte)189,(byte)69)),
                new SolidColorBrush(Color.FromRgb((byte)75,(byte)179,(byte)221)),
                new SolidColorBrush(Color.FromRgb((byte)65,(byte)100,(byte)165)),
                new SolidColorBrush(Color.FromRgb((byte)225,(byte)32,(byte)38)),
                new SolidColorBrush(Color.FromRgb((byte)128,(byte)0,(byte)128)),
                new SolidColorBrush(Color.FromRgb((byte)0,(byte)128,(byte)64)),
                new SolidColorBrush(Color.FromRgb((byte)0,(byte)148,(byte)255)),
                new SolidColorBrush(Color.FromRgb((byte)255,(byte)0,(byte)199)),
                new SolidColorBrush(Color.FromRgb((byte)255,(byte)135,(byte)15)),
                new SolidColorBrush(Color.FromRgb((byte)127,(byte)0,(byte)55))
    
            };

        private static int _currentIndex = new Random(DateTime.Now.Millisecond).Next(0, TileColors.Length);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return GetRandomBackground();
        }

        public static Brush GetRandomBackground()
        {
            int index;

            lock (TileColors)
            {
                index = (_currentIndex++) % TileColors.Length;
            }

            return TileColors[index++];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }
    }
}
