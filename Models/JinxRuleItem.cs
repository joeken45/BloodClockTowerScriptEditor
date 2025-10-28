using CommunityToolkit.Mvvm.ComponentModel;

namespace BloodClockTowerScriptEditor.Models
{
    /// <summary>
    /// 偵測到的相剋規則項目（用於選擇視窗）
    /// </summary>
    public class JinxRuleItem : ObservableObject
    {
        private bool _isSelected;

        /// <summary>
        /// 相剋規則 ID（集石格式的 ID，如 "damsel_poisoner_meta"）
        /// </summary>
        public string RuleId { get; set; } = "";

        /// <summary>
        /// 角色 1 的 ID
        /// </summary>
        public string Role1Id { get; set; } = "";

        /// <summary>
        /// 角色 1 的名稱
        /// </summary>
        public string Role1Name { get; set; } = "";

        /// <summary>
        /// 角色 2 的 ID
        /// </summary>
        public string Role2Id { get; set; } = "";

        /// <summary>
        /// 角色 2 的名稱
        /// </summary>
        public string Role2Name { get; set; } = "";

        /// <summary>
        /// 相剋規則說明
        /// </summary>
        public string Reason { get; set; } = "";

        /// <summary>
        /// 是否可選（已存在則不可選）
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 是否被勾選
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }
}