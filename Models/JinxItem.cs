using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace BloodClockTowerScriptEditor.Models
{
    /// <summary>
    /// Jinx 項目類別 (UI 綁定用)
    /// </summary>
    public class JinxItem : ObservableObject
    {
        private string _targetRoleName = string.Empty;
        private string _reason = string.Empty;

        /// <summary>
        /// 目標角色名稱
        /// </summary>
        public string TargetRoleName
        {
            get => _targetRoleName;
            set => SetProperty(ref _targetRoleName, value);
        }

        /// <summary>
        /// Jinx 規則說明
        /// </summary>
        public string Reason
        {
            get => _reason;
            set => SetProperty(ref _reason, value);
        }

        /// <summary>
        /// 建構子
        /// </summary>
        public JinxItem()
        {
        }

        /// <summary>
        /// 建構子（帶參數）
        /// </summary>
        public JinxItem(string targetRoleName, string reason)
        {
            TargetRoleName = targetRoleName;
            Reason = reason;
        }
    }
}