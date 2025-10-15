using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BloodClockTowerScriptEditor.Converters
{
    /// <summary>
    /// 字串轉 ImageSource 轉換器
    /// 處理空字串或 null 的情況
    /// </summary>
    public class StringToImageSourceConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return null;
            }

            try
            {
                string url = value.ToString()!;
                return new BitmapImage(new Uri(url, UriKind.RelativeOrAbsolute));
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}