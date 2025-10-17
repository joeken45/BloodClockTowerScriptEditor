using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace BloodClockTowerScriptEditor.Models
{
    /// <summary>
    /// 提示標記項目類別
    /// 用於支援勾選刪除功能
    /// </summary>
    public class ReminderItem : ObservableObject
    {
        private string _text = string.Empty;
        private bool _isSelected;

        /// <summary>
        /// 標記文字內容
        /// </summary>
        public string Text
        {
            get => _text;
            set => SetProperty(ref _text, value);
        }

        /// <summary>
        /// 是否被勾選（用於刪除）
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        /// <summary>
        /// 建構子
        /// </summary>
        public ReminderItem()
        {
        }

        /// <summary>
        /// 建構子（帶文字）
        /// </summary>
        /// <param name="text">標記文字</param>
        public ReminderItem(string text)
        {
            Text = text;
        }

        // ==================== 靜態輔助方法 ====================

        /// <summary>
        /// 新增一般提示標記
        /// </summary>
        public static void AddReminder(ObservableCollection<ReminderItem> reminders)
        {
            reminders.Add(new ReminderItem("新標記"));
        }

        /// <summary>
        /// 新增全局提示標記
        /// </summary>
        public static void AddGlobalReminder(ObservableCollection<ReminderItem> remindersGlobal)
        {
            remindersGlobal.Add(new ReminderItem("新全局標記"));
        }

        /// <summary>
        /// 刪除勾選的提示標記
        /// </summary>
        /// <param name="reminders">提示標記集合</param>
        /// <returns>是否成功刪除（true 表示有刪除項目）</returns>
        public static bool RemoveSelected(ObservableCollection<ReminderItem> reminders)
        {
            var toRemove = reminders.Where(r => r.IsSelected).ToList();

            if (toRemove.Count == 0)
            {
                MessageBox.Show(
                    "請先勾選要刪除的標記",
                    "提示",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                return false;
            }

            foreach (var item in toRemove)
            {
                reminders.Remove(item);
            }

            return true;
        }
    }
}