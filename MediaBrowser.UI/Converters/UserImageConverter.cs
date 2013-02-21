using MediaBrowser.Model.DTO;
using MediaBrowser.UI.Controller;
using System;
using System.Globalization;
using System.Net.Cache;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace MediaBrowser.UI.Converters
{
    public class UserImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var user = value as DtoUser;

            if (user != null && user.HasImage)
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

                var uri = UIKernel.Instance.ApiClient.GetUserImageUrl(user.Id, width, height, maxWidth, maxHeight, 100);

                return new BitmapImage(new Uri(uri), new RequestCachePolicy(RequestCacheLevel.Revalidate));
            }

            return null;
        }

        private int? GetSize(string val)
        {
            if (string.IsNullOrEmpty(val) || val == "0")
            {
                return null;
            }

            return int.Parse(val);
        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
