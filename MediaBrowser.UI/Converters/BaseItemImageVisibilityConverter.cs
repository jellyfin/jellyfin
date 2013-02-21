using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using System;
using System.Windows;
using System.Windows.Data;

namespace MediaBrowser.UI.Converters
{
    /// <summary>
    /// Class BaseItemImageVisibilityConverter
    /// </summary>
    class BaseItemImageVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A converted value. If the method returns null, the valid null value is used.</returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var item = value as BaseItemDto;

            if (item != null)
            {
                var paramString = parameter as string;

                var vals = paramString.Split(',');

                var imageType = (ImageType)Enum.Parse(typeof(ImageType), vals[0], true);
                bool reverse = vals.Length > 1 && vals[1].Equals("reverse", StringComparison.OrdinalIgnoreCase);

                return GetVisibility(item, imageType, reverse);
            }

            return Visibility.Collapsed;
        }

        /// <summary>
        /// Gets the visibility.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="type">The type.</param>
        /// <param name="reverse">if set to <c>true</c> [reverse].</param>
        /// <returns>Visibility.</returns>
        private Visibility GetVisibility(BaseItemDto item, ImageType type, bool reverse)
        {
            var hasImageVisibility = reverse ? Visibility.Collapsed : Visibility.Visible;
            var hasNoImageVisibility = reverse ? Visibility.Visible : Visibility.Collapsed;

            if (type == ImageType.Logo)
            {
                return item.HasLogo || !string.IsNullOrEmpty(item.ParentLogoItemId) ? hasImageVisibility : hasNoImageVisibility;
            }

            return item.HasPrimaryImage ? hasImageVisibility : hasNoImageVisibility;
        }

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value that is produced by the binding target.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A converted value. If the method returns null, the valid null value is used.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
