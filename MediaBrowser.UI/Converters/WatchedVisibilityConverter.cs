using MediaBrowser.Model.Dto;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MediaBrowser.UI.Converters
{
    public class WatchedVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as BaseItemDto;

            if (item == null)
            {
                return null;
            }

            if (item.IsFolder)
            {
                return item.PlayedPercentage.HasValue && item.PlayedPercentage.Value == 100 ? Visibility.Visible : Visibility.Collapsed;
            }

            if (item.UserData == null)
            {
                return Visibility.Collapsed;
            }

            return item.UserData.PlayCount == 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class FavoriteVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as BaseItemDto;

            if (item == null)
            {
                return null;
            }

            if (item.UserData == null)
            {
                return Visibility.Collapsed;
            }

            return item.UserData.IsFavorite ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class LikeVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as BaseItemDto;

            if (item == null)
            {
                return null;
            }

            if (item.UserData == null)
            {
                return Visibility.Collapsed;
            }

            var userdata = item.UserData;

            return userdata.Likes.HasValue && userdata.Likes.Value && !userdata.IsFavorite ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DislikeVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as BaseItemDto;

            if (item == null)
            {
                return null;
            }

            if (item.UserData == null)
            {
                return Visibility.Collapsed;
            }

            var userdata = item.UserData;

            return userdata.Likes.HasValue && !userdata.Likes.Value && !userdata.IsFavorite ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
