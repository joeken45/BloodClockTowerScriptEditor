using System;
using System.Globalization;
using System.Windows.Data;

namespace BloodClockTowerScriptEditor.Converters
{
    /// <summary>
    /// 整數大於指定值轉換器
    /// </summary>
    public class IntGreaterThanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue && parameter is string paramStr && int.TryParse(paramStr, out int threshold))
            {
                return intValue > threshold;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}