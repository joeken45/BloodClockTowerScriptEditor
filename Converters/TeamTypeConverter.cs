using BloodClockTowerScriptEditor.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace BloodClockTowerScriptEditor.Converters
{
    /// <summary>
    /// TeamType 轉換為中文顯示名稱
    /// </summary>
    public class TeamTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TeamType team)
            {
                return team switch
                {
                    TeamType.Townsfolk => "鎮民",
                    TeamType.Outsider => "外來者",
                    TeamType.Minion => "爪牙",
                    TeamType.Demon => "惡魔",
                    TeamType.Traveler => "旅行者",
                    TeamType.Fabled => "傳奇",
                    TeamType.Loric => "奇遇",
                    TeamType.Jinxed => "相剋",
                    _ => "未知"
                };
            }
            return value?.ToString() ?? "未知";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string teamName)
            {
                return teamName switch
                {
                    "鎮民" => TeamType.Townsfolk,
                    "外來者" => TeamType.Outsider,
                    "爪牙" => TeamType.Minion,
                    "惡魔" => TeamType.Demon,
                    "旅行者" => TeamType.Traveler,
                    "傳奇" => TeamType.Fabled,
                    "奇遇" => TeamType.Loric, 
                    "相剋" => TeamType.Jinxed,
                    _ => TeamType.Townsfolk
                };
            }
            return TeamType.Townsfolk;
        }
    }

    /// <summary>
    /// 檢查物件是否為 null 的轉換器
    /// </summary>
    public class NullToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 驗證字串是否為空
    /// </summary>
    public class StringNotEmptyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !string.IsNullOrWhiteSpace(value as string);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}