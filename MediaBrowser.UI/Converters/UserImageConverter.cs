using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Net;
using System;
using System.Globalization;
using System.Windows.Data;

namespace MediaBrowser.UI.Converters
{
    /// <summary>
    /// Class UserImageConverter
    /// </summary>
    public class UserImageConverter : IValueConverter
    {
        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A converted value. If the method returns null, the valid null value is used.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var user = value as UserDto;

            if (user != null && user.HasPrimaryImage)
            {
                var config = parameter as string;

                int? maxWidth = null;
                int? maxHeight = null;
                int? width = null;
                int? height = null;

                if (!string.IsNullOrEmpty(config))
                {
                    var vals = config.Split(',');

                    width = GetSize(vals[0]);
                    height = GetSize(vals[1]);
                    maxWidth = GetSize(vals[2]);
                    maxHeight = GetSize(vals[3]);
                }

                var uri = App.Instance.ApiClient.GetUserImageUrl(user, new ImageOptions
                {
                    Width = width,
                    Height = height,
                    MaxWidth = maxWidth,
                    MaxHeight = maxHeight,
                    Quality = 100
                });

                try
                {
                    return App.Instance.GetRemoteBitmapAsync(uri).Result;
                }
                catch (HttpException)
                {
                    
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the size.
        /// </summary>
        /// <param name="val">The val.</param>
        /// <returns>System.Nullable{System.Int32}.</returns>
        private int? GetSize(string val)
        {
            if (string.IsNullOrEmpty(val) || val == "0")
            {
                return null;
            }

            return int.Parse(val);
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
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
