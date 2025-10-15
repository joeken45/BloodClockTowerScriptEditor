using CommunityToolkit.Mvvm.ComponentModel;

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
    }
}