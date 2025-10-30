using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace BloodClockTowerScriptEditor.Models
{
    /// <summary>
    /// Jinx 項目類別 (UI 綁定用)
    /// </summary>
    public class JinxItem : ObservableObject
    {
        private string _targetRolesId = string.Empty;
        private string _reason = string.Empty;

        /// <summary>
        /// 目標角色名稱
        /// </summary>
        public string TargetRoleId
        {
            get => _targetRolesId;
            set => SetProperty(ref _targetRolesId, value);
        }

        /// <summary>
        /// Jinx 規則說明
        /// </summary>
        public string Reason
        {
            get => _reason;
            set
            {
                if (_reason != value)
                {
                    System.Diagnostics.Debug.WriteLine($"🔔 JinxItem.Reason 變更: TargetRole={TargetRoleId}, 舊值={_reason}, 新值={value}");
                    _reason = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 建構子（帶參數）
        /// </summary>
        public JinxItem(string targetRoleId, string reason)
        {
            TargetRoleId = targetRoleId;
            Reason = reason;
        }
    }
}